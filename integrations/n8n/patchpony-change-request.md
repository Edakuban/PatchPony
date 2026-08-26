# n8n Change-Request Intake

Importiere [`patchpony-change-request.json`](patchpony-change-request.json) in `n8n.oi.destination.one`. Der Workflow ist absichtlich deaktiviert.

Vor einem späteren Aktivieren:

1. Für den Webhook ein eigenes Header-Auth-Credential in n8n anlegen. Dieses Credential bleibt außerhalb der JSON-Datei.
2. Den aufrufenden Zoho-/Automationsschritt auf den geschlossenen Envelope festlegen:

   ```json
   {"task":{"id":"1362699000036844130","revision":"17","projectId":"1362699000013318565","channel":"change_request"},"environment":"Staging","kind":"Configuration","targets":[{"reference":"config/retries","requestedValue":"3"}]}
   ```

3. Den späteren PatchPony-Handoff mit dem n8n-Service-Account aus [`patchpony-service-account.md`](patchpony-service-account.md) verbinden. Die dafür vorgesehenen serverseitigen Policies aus I12.2–I12.6 bleiben die maßgebliche Entscheidungsebene.

Der Template-Workflow akzeptiert nur `change_request`, `Configuration`, eine der drei bekannten Umgebungen sowie 1–16 sichere Zielreferenzen mit begrenzten Sollwerten. Er speichert keine Credentials, aktiviert keine Tools und führt keine Patch-, Git-, Shell- oder Zoho-Operation aus.

Nach erfolgreichem kontrollierten Policy-Handoff erzeugt `ChangeRequestCommentFactory` ausschließlich einen statusorientierten Task-Kommentar. Die Zustellung erfolgt allein über den I11.13-Adapter mit lokal konfiguriertem Zoho-OAuth-Token; das n8n-Template besitzt keinen Zoho-Token und kann daher keine beliebigen Kommentare schreiben.