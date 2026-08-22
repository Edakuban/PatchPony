# MCP-Tools

MCP-Tools werden als Gateway-Adapter über den offiziellen C#-SDK-Mechanismus registriert. Fach- und Sicherheitslogik bleibt außerhalb der Toolklasse; ein Tool darf nur einen vorhandenen Core-Anwendungsfall oder Core-Service aufrufen.

Der erste stabile, rein lesende Vertrag ist `runtime.status`:

| Eigenschaft | Wert |
| --- | --- |
| Zweck | Liefert Dienstname, aktuellen read-only-Modus und die Korrelations-ID des Requests. |
| Zugriff | Kein Projekt-, Dateisystem-, Netzwerk- oder externer Toolzugriff. |
| MCP-Annotationen | read-only, idempotent, nicht destruktiv, keine Open-World-Interaktion. |
| Core-Zuordnung | `RuntimeStatusService` mit `ICorrelationContext` aus dem Core. |

Die Tool-Discovery und ein tatsächlicher `tools/call` sind als Gateway-Integrationstest abgedeckt. Der äquivalente n8n-REST-Endpunkt ist `GET /api/v1/runtime/status`. Projekt-, Source- und Knowledge-Tools folgen, sobald ihre Gateway-Komposition eingeführt wird.

`runtime.validate_correlation` ist der validierende Gegenpart und liefert bei ungültiger Eingabe den gemeinsamen Fehlervertrag aus [api-errors.md](api-errors.md) als MCP-Toolfehler.
