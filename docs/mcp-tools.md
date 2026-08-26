# MCP-Tools

MCP-Tools werden als Gateway-Adapter über den offiziellen C#-SDK-Mechanismus registriert. Fach- und Sicherheitslogik bleibt außerhalb der Toolklasse; ein Tool darf nur einen vorhandenen Core-Anwendungsfall oder Core-Service aufrufen.

Der erste stabile, rein lesende Vertrag ist `runtime.status`:

| Eigenschaft | Wert |
| --- | --- |
| Zweck | Liefert Dienstname, aktuellen read-only-Modus und die Korrelations-ID des Requests. |
| Zugriff | Kein Projekt-, Dateisystem-, Netzwerk- oder externer Toolzugriff. |
| MCP-Annotationen | read-only, idempotent, nicht destruktiv, keine Open-World-Interaktion. |
| Core-Zuordnung | `RuntimeStatusService` mit `ICorrelationContext` aus dem Core. |

Die Tool-Discovery und ein tatsächlicher `tools/call` sind als Gateway-Integrationstest abgedeckt. Der äquivalente n8n-REST-Endpunkt ist `GET /api/v1/runtime/status`. Projekt- und Source-Tools folgen mit ihrer jeweiligen Gateway-Komposition. Die Knowledge-Leseverträge `knowledge.tree`, `knowledge.search`, `knowledge.read` und `knowledge.links` sind bereits registriert; Details stehen in [knowledge-contracts.md](knowledge-contracts.md).

`runtime.validate_correlation` ist der validierende Gegenpart und liefert bei ungültiger Eingabe den gemeinsamen Fehlervertrag aus [api-errors.md](api-errors.md) als MCP-Toolfehler.
## Schema-Verträge

Die Tool-Discovery ist ein versionsgebundener Vertrag. Ein Gateway-Integrationstest
fixiert für `runtime.status` und `runtime.validate_correlation` die vollständige
Toolmenge, Namen, Titel, Sicherheitsannotationen und JSON-Schemas. Für
`runtime.status` gehören `service`, `mode` und `correlationId` verpflichtend zum
strukturierten Ergebnis; `runtime.validate_correlation` verlangt den String-Parameter
`correlationId`. Änderungen daran sind eine bewusst zu versionierende API-Änderung.

## Sandbox-Tests

`tests.list` listet nur für eine autorisierte Session serverseitig registrierte Command-IDs. `tests.run` akzeptiert ausschließlich eine dieser IDs und legt einen privaten Worker-Handoff an; Image, Executable, Argumente, Docker-Optionen und Hostpfade gehören nicht zum MCP-Vertrag. `tests.result` liefert ausschließlich ein bereits durch den Worker gespeichertes, begrenztes Ergebnis.

Alle drei Tools verlangen den Scope `tests:run`; `tests.run` ist nicht idempotent, `tests.list` und `tests.result` sind read-only und idempotent.