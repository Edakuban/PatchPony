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