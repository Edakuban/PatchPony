# I6 – Pilotentscheidungen

Stand: 2026-08-22

Diese Entscheidungen gelten für den ersten read-only Pilot und begrenzen die Umsetzung von I6. Sie sind keine Produktionsfreigabe.

| Thema | Entscheidung für den Pilot |
|---|---|
| Chat-Oberfläche | Open WebUI läuft unter `oi.destination.one`. |
| Workflow | n8n läuft unter `n8n.oi.destination.one`; Endnutzer benötigen keinen direkten n8n-Zugang. |
| Föderierte Anmeldung | Gegenüber Open WebUI und n8n wird später `id.destination.one` (lokaler Keycloak) verwendet. |
| PatchPony-Container | Bis zur SSO-Integration sichern lokale Gateway-/Worker-Tests interne Aufrufe mit dem Development-Passwort beziehungsweise getrennten Service-Account-Token ab. |
| Open WebUI → n8n | Die Open-WebUI-Pipe ruft ausschließlich den vertrauenswürdigen HTTPS-Webhook von `n8n.oi.destination.one` auf. |
| n8n → PatchPony | n8n prüft Zoho-Aufgaben und führt die freigegebenen AI-Agent-Prompts einschließlich MCP-Aufrufen aus. Der dafür bestimmte Service-Account folgt in I6.2. |
| Modelle | Für den ersten Test dient `gpt-oss:20b` über `oi.destination.one` als Standardmodell. Weitere Modelle bleiben konfigurierbar. |
| Modellzugang | Für eine später benötigte interne KI-Funktion nutzt PatchPony den bereits lokal hinterlegten Provider-API-Key; bis zu einer expliziten Entscheidung wird keine interne Modellfunktion implementiert. |
| Datenhaltung | Prompts, Antworten und Workflow-Metadaten dürfen für interne Mitarbeitende gespeichert werden. Löschfristen werden bewusst später entschieden. |
| Pilot-Projektkontext | `DEFAULT_PROJECT_ID` wird ausschließlich als globale Open-WebUI-Admin-Einstellung gesetzt und muss in der n8n-Allowlist stehen. Er ist nur Startkontext; ein projektübergreifender Wechsel benötigt `projects.list` und bleibt auf die serverseitige n8n-Allowlist begrenzt. |
| Pilot-Identität | Die Pipe leitet nur `owui-sha256:<hash>` aus der Open-WebUI-ID weiter. Keycloak/OIDC-Subjekte ersetzen dieses Übergangsformat später. |

## Sicherheitsgrenzen des Piloten

- Keine API-Keys, Passwörter oder Tokens in Git, Pipe-Quelltext, Logs oder Dokumentation.
- Der n8n-Webhook muss HTTPS nutzen und ist auf den konkreten vertrauenswürdigen Host zu setzen.
- Die Pipe übergibt weiterhin nur die letzte Frage. Benutzeridentität und Projektwahl folgen erst kontrolliert in I6.4.
- Der Gateway bleibt für externe Aufrufe Default-Deny; n8n erhält einen separaten, minimal berechtigten Service-Account.

## Noch offen vor Produktion

- Keycloak-/OIDC-Claims, Rollen-Mapping und Gruppenverwaltung.
- Authentisierung des Webhooks zwischen Open WebUI und n8n.
- Aufbewahrungs- und Löschkonzept.
- Freigegebene Modellliste und Routing-Regeln.
- Datenschutzprüfung des tatsächlich übertragenen Kontexts (I6.10).