# Gateway-Limits

Für MCP (`/mcp`) und die versionierte REST-API (`/api/v1`) erzwingt das Gateway feste, serverseitige Grenzen. Health-Checks bleiben davon ausgenommen.

| Schutz | Standardgrenze | Reaktion |
| --- | ---: | --- |
| Request-Target | 4.096 Zeichen | `414`, `request.target_too_large` |
| Request-Body | 64 KiB | `413`, `request.body_too_large` |
| Ergebnis | 256 KiB | `413`, `response.too_large` |
| Gleichzeitige API-Requests | 8 | `429`, `request.concurrency_limited` |
| REST-Requests pro Subjekt | 60 / 60 Sekunden | `429`, `request.rate_limited`, `Retry-After` |
| MCP-Requests pro Subjekt | 30 / 60 Sekunden | `429`, `request.rate_limited`, `Retry-After` |

Die Ablehnungen verwenden den gemeinsamen Fehlervertrag aus [api-errors.md](api-errors.md), einschließlich der Korrelations-ID.

## Rate-Limit-Partitionen

Der Fixed-Window-Zähler wird für authentifizierte Aufrufe über `sub` beziehungsweise
`NameIdentifier` partitioniert. Anonyme Aufrufe werden konservativ nach der beim
Gateway sichtbaren Gegenstelle gruppiert. REST und MCP haben getrennte Kontingente;
damit kann ein Transport den anderen nicht aufbrauchen oder umgehen. Es gibt keine
Warteschlange: Ist ein Fenster ausgeschöpft, antwortet PatchPony sofort mit `429`
und `Retry-After` in Sekunden.

Die Werte können über diese Umgebungsvariablen gesetzt werden:

```text
PATCHPONY_RATELIMITING__APIPERMITLIMIT=60
PATCHPONY_RATELIMITING__MCPPERMITLIMIT=30
PATCHPONY_RATELIMITING__WINDOWSECONDS=60
PATCHPONY_RATELIMITING__MAXIMUMTRACKEDPARTITIONS=1000
```

Der Speicher für maximal 1.000 Partitionen ist absichtlich begrenzt. Neue
Partitionen werden bei voller Tabelle ebenfalls abgewiesen, statt durch rotierende
Client-IDs unbegrenzt Speicher zu belegen. Die Zähler gelten pro Gateway-Prozess;
eine spätere horizontale Skalierung benötigt einen gemeinsamen, externen
Rate-Limit-Store oder eine vorgeschaltete Gateway-Rate-Limit-Policy.