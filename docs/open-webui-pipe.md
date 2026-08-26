# Open WebUI → n8n Pipe

Die Datei [`integrations/open-webui/patchpony_n8n_pipe.py`](../integrations/open-webui/patchpony_n8n_pipe.py) ist eine Open-WebUI-Pipe-Function für den read-only PatchPony-Pilot. Sie erscheint im Modellwähler als **PatchPony · Read-only** und leitet ausschließlich die letzte Benutzerfrage an n8n weiter.

## Installation

1. In Open WebUI als Administrator **Workspace → Functions** öffnen.
2. Eine neue Function anlegen und den vollständigen Inhalt der Pipe-Datei einfügen.
3. Die Function aktivieren.
4. In den Valve-Einstellungen `N8N_WEBHOOK_URL`, `N8N_WEBHOOK_TOKEN` und `DEFAULT_PROJECT_ID` setzen. Die Projekt-ID wird ausschließlich durch einen Administrator gesetzt und muss auch in der n8n-Service-Account-Allowlist stehen.
5. Im Chat das Modell **PatchPony · Read-only** auswählen.

Der Webhook-Token ist ein eigener, zufälliger Secret-Wert und wird ausschließlich an den konfigurierten n8n-Host gesendet.

Die Pipe erwartet von n8n eine synchrone JSON-Antwort in dieser Form:

```json
{ "answer": "Antwort für den Entwickler", "sources": [{ "projectId": "patchpony", "path": "README.md", "startLine": 1, "endLine": 3 }], "toolCalls": ["source.read"] }
```

An n8n geht nur dieser minimale Vertrag:

```json
{
  "requestId": "UUID",
  "source": "open-webui",
  "question": "letzte Benutzerfrage"
}
```

Die Pipe übermittelt weder den vollständigen Chatverlauf noch Klartext-Open-WebUI-Benutzerattribute, Modellprovider-Schlüssel oder Gateway-/n8n-Service-Credentials. Die Projektwahl ist eine globale Admin-Einstellung; als Pilot-Identität wird ausschließlich ein SHA-256-pseudonymisiertes Open-WebUI-Subjekt weitergegeben. Keycloak/OIDC ersetzt dieses Pilotformat später; die n8n-zu-Gateway-Authentifizierung folgt in I6.2. Verifizierte Quellen und verwendete, erlaubte Read-only-Tools werden unterhalb der Antwort angezeigt. Fehlende Konfiguration, Timeouts, Abbrüche, Rate Limits und Upstream-Fehler ergeben nur sichere, nutzerfreundliche Fehlermeldungen ohne Infrastrukturdetails. Abgebrochene Pipe-Tasks werden nicht verschluckt, sondern an Open WebUI zurückgegeben.

Open-WebUI-Functions laufen serverseitig mit den Rechten des Open-WebUI-Prozesses. Deshalb darf die Pipe ausschließlich von Administratoren importiert, vor dem Aktivieren geprüft und nur mit einer vertrauenswürdigen HTTPS-n8n-URL konfiguriert werden. Die Pilotvorgaben stehen in [i6-pilot-decisions.md](i6-pilot-decisions.md).