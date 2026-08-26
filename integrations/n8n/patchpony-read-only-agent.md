# n8n Read-only Agent Workflow

Importiere [`patchpony-read-only-agent.json`](patchpony-read-only-agent.json) in `n8n.oi.destination.one`. Der Workflow ist nach dem Import absichtlich deaktiviert.

## Einmalige Konfiguration

1. Aus I6.2 das Header-Auth-Credential **PatchPony Service Account** anlegen.
2. Das OpenAI-kompatible Credential **destination.one OpenAI-compatible** anlegen: vorhandenen destination.one-Endpunkt und API-Key verwenden, danach `gpt-oss:20b` auswählen. Credentials gehören ausschließlich in n8n, nie in Workflow-JSON.
3. `DEFAULT_PROJECT_ID` in der Open-WebUI-Pipe auf eine konkrete ID setzen und dieselbe ID in `PATCHPONY_AUTH__N8N__PROJECTS` erlauben. Nutzer können diese Projektbindung nicht selbst wählen.
4. In den n8n-Umgebungsvariablen `PATCHPONY_GATEWAY_URL=https://<patchpony-host>` setzen.
5. Das zweite Header-Auth-Credential **Open WebUI PatchPony Webhook (X-PatchPony-Webhook-Token)** anlegen: Header-Name `X-PatchPony-Webhook-Token`, Wert ein eigener zufälliger Secret. Dasselbe Secret ausschließlich als `N8N_WEBHOOK_TOKEN` in den Valve-Einstellungen der Open-WebUI-Pipe hinterlegen.
6. Test-URL ausführen, eine gültige Pipe-Payload senden und anschließend die Production-URL aktivieren.

## Ablauf und Grenzen

`Open WebUI webhook` → Payload-Validierung → `Read-only PatchPony Agent` → sichere JSON-Antwort.

Der Agent erhält nur die Frage; n8n behält die bereits validierte Projekt-ID und das pseudonyme Anforderer-Subjekt für die kontrollierte nächste Tool-Stufe. Als MCP-Tools sind ausdrücklich nur `runtime.status`, `runtime.validate_correlation`, `source.search` und `source.read` eingebunden; es gibt keine Schreib-, Shell-, Git-, Zoho- oder beliebigen HTTP-Tools. Der System-Prompt verbietet diese Operationen zusätzlich. Die n8n-MCP-Client-Tool-Konfiguration verwendet Streamable HTTP unter `/mcp` und das Header-Credential aus I6.2.

Der Agent hat serverseitig `maxIterations: 6`. Jede Iteration kann höchstens einen Toolaufruf ausführen; damit sind pro Anfrage höchstens sechs erlaubte MCP-Toolaufrufe möglich. Die Anzeige begrenzt die zurückgegebenen Toolnamen zusätzlich auf sechs. Das Gateway hält unabhängig davon sein MCP-Kontingent von 30 Requests je Service-Account und 60 Sekunden ein.

Die Payload-Validierung akzeptiert nur eine Admin-Projekt-ID und ein Format `owui-sha256:<hash>`; Roh-Open-WebUI-Identitäten werden abgelehnt.

Für I6.6 aktiviert der Agent die Zwischenstände. Der anschließende Format-Schritt gibt davon ausschließlich eine Allowlist der tatsächlich verwendeten read-only Toolnamen und Zitierungen aus Source-Toolresultaten aus. Keine Toolparameter, Inhalte, Fehlerdetails, Credentials oder internen n8n-Daten werden an Open WebUI weitergegeben.

Der Agent läuft mit `onError: continueRegularOutput`. Der Format-Schritt wandelt nur Iterationslimit, Timeout, Abbruch und Rate Limit in vorgegebene deutsche Nutzertexte um; alle anderen Fehler erhalten ebenfalls eine generische Antwort. Rohfehler, Stacktraces, Toolparameter und Infrastrukturdetails verlassen n8n nicht.

Die Workflow-Ausführungen speichern weder Erfolg- noch Fehlerdaten. Der Zoho-Trigger folgt in einer späteren Stufe. Vor einem produktiven Aktivieren müssen Webhook-Authentisierung, Keycloak-Integration und I6.10 abgeschlossen sein.