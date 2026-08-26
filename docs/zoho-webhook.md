# Zoho-Task-Webhook

Der Eingang für Zoho-Aufgaben ist `POST /api/v1/integrations/zoho/tasks`. Er ist bewusst nicht über die allgemeine Benutzer- oder n8n-Authentifizierung erreichbar, sondern prüft den unveränderten Request-Body mit `X-PatchPony-Zoho-Signature`.

`PatchPony__Zoho__WebhookSecret` liegt nur in `.env` oder einem Secret Store. Der Header enthält den hexadezimalen HMAC-SHA-256 als `sha256=<64 lowercase hex>`. Ohne mindestens 32 Zeichen Secret antwortet der Endpoint fail-closed mit `503`; ungültige oder mehrdeutige Signaturen erhalten `400`. Weder Ticketinhalt noch Secret werden zurückgegeben oder geloggt.

n8n normalisiert die Zoho-Aufgabe vor dem Versand auf den folgenden geschlossenen Vertrag:

```json
{"eventType":"task.created","task":{"id":"1362699000036844130","revision":"17","projectId":"1362699000013318565","channel":"change_request","title":"Kurzbeschreibung","description":"Details"}}
```

Erlaubte Events sind `task.created` und `task.updated`. `channel` ist eine bewusst kleine Normalisierung des sichtbaren Zoho-Custom-Fields: `generic_task`, `feature_request`, `bug` oder `change_request`. Die technische Zoho-Custom-Field-ID bleibt n8n-Detail und wird nicht als freie Eingabe in PatchPony übernommen.

Task-ID, Revision und Projekt-ID sind auf 128 Zeichen aus Buchstaben, Ziffern, `.`, `_`, `-` begrenzt; Titel auf 500, Beschreibung auf 16 KiB. Optional sind reine Attachment-Metadaten erlaubt. Die Projektzuordnung nutzt ausschließlich `task.projectId`, nicht Namens- oder Präfixheuristiken. Der Endpoint bestätigt nur mit `202 { "received": true }`. Es wird noch kein Job erzeugt und kein Kommentar an Zoho geschrieben.