# OpenAPI und Swagger

PatchPony stellt das aktuelle, maschinenlesbare REST-Dokument unter
`GET /openapi/v1.json` bereit. Es beschreibt ausschließlich die versionierte
REST-API unter `/api/v1`; der MCP-Endpunkt `/mcp` ist bewusst kein REST-Teil
dieses Dokuments.

## Lokale Swagger UI

Die interaktive Swagger UI wird ausschließlich in der Entwicklungsumgebung
registriert. Nach dem lokalen Start des Gateways ist sie unter
[http://localhost:5055/swagger](http://localhost:5055/swagger) erreichbar.

```powershell
dotnet run --project src/PatchPony.Gateway/PatchPony.Gateway.csproj
```

In Produktion liefert `/swagger/index.html` bewusst `404`. Das OpenAPI-JSON
bleibt für versionierte Client-Integration und externe Dokumentationswerkzeuge
verfügbar.

Der Integrationstest `OpenApiDocument_DescribesTheVersionedRestContract`
prüft Titel, Version, Operation-IDs und alle aktuellen `/api/v1`-Pfade.
## Docker Development

The local Compose stack sets `PATCHPONY_ASPNETCORE_ENVIRONMENT=Development` by default. After `docker compose up --build`, Swagger is available through the local Caddy TLS endpoint at `https://localhost:8443/swagger`; the OpenAPI document is `https://localhost:8443/openapi/v1.json`. The Rootless production overlay forcibly sets `Production`, so Swagger remains unavailable there.

## Direct local Gateway diagnostic

When diagnosing local TLS/proxy behavior, use the development-only overlay. It binds the Gateway to loopback HTTP only and is never part of the Rootless production overlay:

```powershell
docker compose -f docker-compose.yml -f deploy/development/docker-compose.gateway-debug.yml up -d --force-recreate gateway
```

Use `http://127.0.0.1:18080/swagger` or the same REST/MCP route on port `18080`. This overlay is for local diagnostics only; normal local use remains the Caddy HTTPS endpoint.