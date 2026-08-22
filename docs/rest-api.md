# REST API

Alle öffentlichen REST-Verträge von PatchPony werden unter dem versionsgebundenen Präfix `/api/v1` bereitgestellt. Unversionierte `/api`-Routen sind absichtlich nicht verfügbar.

`GET /api/v1` ist ein kleiner Discovery-Endpunkt und liefert die API-Version, den Dienstnamen und den aktuellen read-only-Modus. Er bildet keine Fachlogik ab.

Die in der Planung vorgesehenen Projekt-, Quelltext- und Knowledge-Routen werden später direkt in derselben Route-Group ergänzt. Dadurch kann eine zukünftige Version wie `/api/v2` parallel eingeführt werden, ohne bestehende n8n- oder andere REST-Clients zu brechen.

## Vorhandene read-only Fähigkeit

`GET /api/v1/runtime/status` ist der REST-Gegenpart zum MCP-Tool `runtime.status`. Beide Adapter delegieren an denselben Core-Handler und liefern Dienstname, read-only-Modus und die Korrelations-ID des Requests.

`GET /api/v1/runtime/correlations/{correlationId}` validiert eine Korrelations-ID. Fehler verwenden den gemeinsamen Vertrag in [api-errors.md](api-errors.md).
