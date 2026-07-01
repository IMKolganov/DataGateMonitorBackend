#!/bin/sh

echo "[entrypoint] Current UID: $(id -u), GID: $(id -g)"

# ✅ Ensure resources directory exists (GeoLite, certs, startup history, …)
echo "[entrypoint] Ensuring /app/resources directory exists..."
mkdir -p /app/resources/geo-lite2
mkdir -p /app/resources/certs
chown -R app:app /app/resources || echo "[entrypoint] chown failed on resources"

echo "[entrypoint] Starting application..."
exec dotnet DataGateMonitor.dll