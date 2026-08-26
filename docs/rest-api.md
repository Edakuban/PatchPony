# REST API

Alle öffentlichen REST-Verträge von PatchPony werden unter dem versionsgebundenen Präfix `/api/v1` bereitgestellt. Unversionierte `/api`-Routen sind absichtlich nicht verfügbar.

`GET /api/v1` ist ein kleiner Discovery-Endpunkt und liefert die API-Version, den Dienstnamen und den aktuellen read-only-Modus. Er bildet keine Fachlogik ab.

Die in der Planung vorgesehenen Projekt- und Quelltext-Routen werden später direkt in derselben Route-Group ergänzt. Die Knowledge-Leserouten sind bereits vorhanden und in [knowledge-contracts.md](knowledge-contracts.md) beschrieben. Dadurch kann eine zukünftige Version wie `/api/v2` parallel eingeführt werden, ohne bestehende n8n- oder andere REST-Clients zu brechen.

## Vorhandene read-only Fähigkeit

`GET /api/v1/runtime/status` ist der REST-Gegenpart zum MCP-Tool `runtime.status`. Beide Adapter delegieren an denselben Core-Handler und liefern Dienstname, read-only-Modus und die Korrelations-ID des Requests.

`GET /api/v1/runtime/correlations/{correlationId}` validiert eine Korrelations-ID. Fehler verwenden den gemeinsamen Vertrag in [api-errors.md](api-errors.md).

Das maschinenlesbare Dokument und die lokale Swagger-UI sind in [openapi.md](openapi.md) beschrieben.
## Authentifizierungsnachweis

`GET /api/v1/runtime/identity` verlangt bereits Authentifizierung. Es dient
lokal zur Prüfung des Entwicklungspassworts und produktiv zur Prüfung eines
OIDC-Bearer-Tokens. Konfiguration und Sicherheitsgrenzen stehen in
[authentication.md](authentication.md).

`POST /api/v1/projects/{projectId}/access` prüft als geschützter Preflight Projekt-, Tool- und Parameterfreigaben. Die Regeln stehen in [authentication.md](authentication.md).

## Zugriffs-Audit (Reviewer)

`GET /api/v1/runtime/audit/access?limit=100` liefert einen begrenzten,
neueste-zuerst sortierten Auszug der Authentifizierungs- und
Projekt-Tool-Entscheidungen. Er verlangt die Rolle `reviewer`. Die Antwort
enthält nur sichere Auswertungsfelder wie Correlation-ID, Entscheidung,
Subjekt, Authentifizierungsmodus, Projekt, Tool und benötigten Scope – nie
Credentials oder Parameterwerte.
## Sandbox-Tests

Für eine autorisierte Projekt-Session stehen diese Routen bereit; alle verlangen `tests:run`:

- `GET /api/v1/projects/{projectId}/sessions/{sessionId}/tests` – registrierte Command-IDs
- `POST /api/v1/projects/{projectId}/sessions/{sessionId}/tests/run` – privater Worker-Handoff für `{ "commandId": "..." }`
- `GET /api/v1/projects/{projectId}/sessions/{sessionId}/tests/{executionId}` – begrenztes, gespeichertes Ergebnis

Der Gateway startet niemals Docker. Die Ausführung erfolgt ausschließlich nach Übergabe an den privaten Worker.