# OpenVpnGateMonitorBackend

## Overview
OpenVPN Gate Monitor is a .NET-based backend service that manages OpenVPN client connections and certificates. It runs inside a Docker container and integrates with EasyRSA.

## Features
- **Automated Certificate Management**
- **Client Monitoring**
- **REST API for OpenVPN Control**
- **Secure Deployment with Docker**

## Installation

### Prerequisites
Ensure Docker and Docker Compose are installed:
```bash
sudo apt-get update && sudo apt-get install -y docker.io docker-compose
```
Check host user ID:
```bash
id ivan
```

## Overview
This guide explains how to correctly set up a non-root user in a Docker container with a specific **UID/GID (1000:1000)** for seamless file access between the container and host.

## Why Use a Specific UID/GID?
By default, files on the host may belong to `ivan` (`uid=1000`, `gid=1000`).  
If the container runs a different user, it might **lack permissions** to access mounted volumes like `/etc/openvpn/easy-rsa`.

## Solution: Set UID/GID in Dockerfile

### Step 1: Define UID/GID in `Dockerfile`
```dockerfile
ARG HOST_UID=1000
ARG HOST_GID=1000

# Ensure group exists before adding
RUN getent group app || groupadd -g $HOST_GID app

# Ensure user exists before adding
RUN id -u app &>/dev/null || useradd -m -u $HOST_UID -g app app

# Set ownership for app directory
RUN mkdir -p /app && chown -R app:app /app

# Switch to non-root user
USER app
WORKDIR /app
```

### Step 2: Pass UID/GID During Build
```bash
docker build --build-arg HOST_UID=1000 --build-arg HOST_GID=1000 -t datagate-monitor-backend .
```

## Verify User in Container
Check user inside the running container:
```bash
docker exec -it datagate-monitor-backend id
```
Expected output:
```
uid=1000(app) gid=1000(app)
```

### Build and Run
Clone the repository:
```bash
git clone https://github.com/your-repo/DataGateMonitor.git && cd DataGateMonitor
```
Build and run with Docker Compose:
```bash
docker build --build-arg HOST_UID=1000 --build-arg HOST_GID=1000 -t datagate-monitor-backend .
docker-compose up -d
```
Check running containers:
```bash
docker ps
```

## Configuration

### Mounted Volumes
```yaml
volumes:
  - /etc/openvpn/easy-rsa:/etc/openvpn/easy-rsa
  - /etc/openvpn/clients:/etc/openvpn/clients
  - /var/log/openvpn-status.log:/var/log/openvpn-status.log:ro
```
Ensure the host user has proper permissions.

### Environment Variables
```yaml
environment:
  - DOTNET_ENVIRONMENT=Debug
  - ASPNETCORE_URLS=http://0.0.0.0:5581
```

## API Endpoints

### List Issued Certificates
```bash
curl -X GET http://localhost:5581/api/certificates
```
### Issue a New Certificate
```bash
curl -X POST http://localhost:5581/api/certificates -d '{"clientName": "GateMonitor1"}' -H "Content-Type: application/json"
```
### Revoke a Certificate
```bash
curl -X DELETE http://localhost:5581/api/certificates/GateMonitor1
```

### Mobile Crash Ingest
`POST /api/v1/mobile/crash-ingest`

Request requirements:
- Content-Type: `text/plain; charset=utf-8`
- Headers:
  - `X-Crash-Filename`
  - `X-Crash-Process`
  - `X-Crash-Token` (optional, when configured)

Response codes:
- `204` success
- `400` invalid/empty body or invalid headers/content-type
- `413` payload too large
- `429` rate-limited
- `500` server error

Backend crash-ingest settings (env-compatible):
- `CrashIngest__MaxPayloadBytes` (default `1048576`)
- `CrashIngest__RateLimitMaxRequests` (default `20`)
- `CrashIngest__RateLimitWindowSeconds` (default `60`)
- `CrashIngest__RequireHttps` (default `true`)
- `CrashIngest__RecentMaxLimit` (default `200`)
- `CrashIngest__AuthToken` (optional)
- `X_CRASH_TOKEN` (optional env override for token)

Android client:
- `CRASH_REPORT_URL=https://<host>/api/v1/mobile/crash-ingest`
- If token mode enabled, send `X-Crash-Token` with the same secret.

Example:
```bash
curl -X POST "https://<host>/api/v1/mobile/crash-ingest" \
  -H "Content-Type: text/plain; charset=utf-8" \
  -H "X-Crash-Filename: fatal_2026-05-01T00-00-00.000Z.txt" \
  -H "X-Crash-Process: com.imkolganov.datagate.dev" \
  --data-binary @sample_crash.txt
```

With token:
```bash
curl -X POST "https://<host>/api/v1/mobile/crash-ingest" \
  -H "Content-Type: text/plain; charset=utf-8" \
  -H "X-Crash-Filename: fatal_2026-05-01T00-00-00.000Z.txt" \
  -H "X-Crash-Process: com.imkolganov.datagate.dev" \
  -H "X-Crash-Token: <your-token>" \
  --data-binary @sample_crash.txt
```

## Debugging

### Attach to a Running Container
```bash
docker exec -it datagate-monitor-backend /bin/sh
```
### Restart the Backend
```bash
docker-compose restart backend
```
### View OpenVPN Logs
```bash
tail -f /var/log/openvpn-status.log
```

## License
MIT License. See `LICENSE` for details.