<h1 align="left">
  <img src="https://raw.githubusercontent.com/IMKolganov/DataGateMonitorFrontend/main/public/favicon.svg" width="32" height="32" alt="" />
  DataGate Monitor — Backend
</h1>

ASP.NET Core API for [DataGate Monitor](https://dash.datagateapp.com/): VPN server overview, client statistics, auth, admin tools, mobile crash ingest, and integration with OpenVPN / Xray sidecars.

Part of the [DataGateMonitor](https://github.com/IMKolganov/DataGateMonitor) monorepo (`backend/` submodule). Standalone repo: [DataGateMonitorBackend](https://github.com/IMKolganov/DataGateMonitorBackend).

## Links

| Resource | Link |
|----------|------|
| <img src="https://raw.githubusercontent.com/IMKolganov/DataGateMonitorFrontend/main/public/favicon.svg" width="16" height="16" alt="" /> **DataGate app** | [datagateapp.com](https://datagateapp.com/) |
| <img src="https://cdn.simpleicons.org/googleplay/414141" width="16" height="16" alt="" /> **Download** | [datagateapp.com/download](https://datagateapp.com/download) |
| <img src="https://cdn.simpleicons.org/grafana/F46800" width="16" height="16" alt="" /> **Dashboard** | [dash.datagateapp.com](https://dash.datagateapp.com/) |
| <img src="https://cdn.simpleicons.org/telegram/26A5E4" width="16" height="16" alt="" /> **Telegram channel** | [@datagateapp](https://t.me/datagateapp) |

## Prerequisites

- .NET SDK (see `global.json` / project `TargetFramework`)
- PostgreSQL
- Optional: Elasticsearch (logging / search)

## Local run

```bash
cd backend/DataGateMonitor
dotnet run
```

Default URL: `http://localhost:5581` (override with `ASPNETCORE_URLS`).

Configure connection strings and secrets via `appsettings.json`, user secrets, or environment variables (`DB_CONNECTION_STRING_DATAGATE`, `JWT_SECRET`, etc.).

## Docker (monorepo)

From the monorepo root:

```bash
docker compose -f docker-compose-local.yml --env-file .env.dev.x64 up -d --build backend
```

Image: `imkolganov/datagate-monitor-backend`.

## Database migrations

```bash
cd backend/DataGateMonitor.DataBase
dotnet ef database update --startup-project ../DataGateMonitor
```

## SharedModels

API DTOs live in the NuGet package `DataGateMonitor.SharedModels`. Do **not** add a project reference to SharedModels from consuming projects — bump the package version after publishing a new SharedModels release.

## Mobile crash ingest

`POST /api/v1/mobile/crash-ingest`

- Content-Type: `text/plain; charset=utf-8`
- Headers: `X-Crash-Filename`, `X-Crash-Process`, optional `X-Crash-Token`

| Code | Meaning |
|------|---------|
| 204 | Success |
| 400 | Invalid body / headers |
| 413 | Payload too large |
| 429 | Rate limited |
| 500 | Server error |

Settings (env): `CrashIngest__MaxPayloadBytes`, `CrashIngest__RateLimitMaxRequests`, `CrashIngest__RateLimitWindowSeconds`, `CrashIngest__RequireHttps`, `CrashIngest__RecentMaxLimit`, `CrashIngest__AuthToken` / `X_CRASH_TOKEN`.

Example:

```bash
curl -X POST "https://<host>/api/v1/mobile/crash-ingest" \
  -H "Content-Type: text/plain; charset=utf-8" \
  -H "X-Crash-Filename: fatal_2026-05-01T00-00-00.000Z.txt" \
  -H "X-Crash-Process: com.imkolganov.datagate.dev" \
  --data-binary @sample_crash.txt
```

## License

MIT

## Author

**Ivan Kolganov**

| Contact | Link |
|---------|------|
| <img src="https://api.iconify.design/simple-icons/linkedin.svg?color=%230A66C2" width="16" height="16" alt="" /> **LinkedIn** | [linkedin.com/in/imkolganov](https://www.linkedin.com/in/imkolganov/?locale=en) |
| <img src="https://cdn.simpleicons.org/telegram/26A5E4" width="16" height="16" alt="" /> **Telegram** | [@KolganovIvan](https://t.me/KolganovIvan) |
| <img src="https://cdn.simpleicons.org/buymeacoffee/FFDD00" width="16" height="16" alt="" /> **Buy Me a Coffee** | [buymeacoffee.com/imkolganov](https://buymeacoffee.com/imkolganov) |
