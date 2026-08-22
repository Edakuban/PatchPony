# Gateway-Limits

Für MCP (`/mcp`) und die versionierte REST-API (`/api/v1`) erzwingt das Gateway feste, serverseitige Grenzen. Health-Checks bleiben davon ausgenommen.

| Schutz | Standardgrenze | Reaktion |
| --- | ---: | --- |
| Request-Target | 4.096 Zeichen | `414`, `request.target_too_large` |
| Request-Body | 64 KiB | `413`, `request.body_too_large` |
| Ergebnis | 256 KiB | `413`, `response.too_large` |
| Gleichzeitige API-Requests | 8 | `429`, `request.concurrency_limited` |

Die Ablehnungen verwenden den gemeinsamen Fehlervertrag aus [api-errors.md](api-errors.md), einschließlich der Korrelations-ID. Die Parallelitätsgrenze wird gemeinsam für MCP und REST gezählt, damit ein Transport den anderen nicht umgehen kann.
