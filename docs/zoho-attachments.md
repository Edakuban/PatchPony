# Zoho-Anhangsgrenzen

Zoho kann optional reine Attachment-Metadaten im Ticket liefern: `id`, `fileName`, `contentType`, `sizeBytes`. PatchPony akzeptiert höchstens fünf Anhänge, 2 MiB je Anhang und 5 MiB insgesamt.

Erlaubt sind nur `text/plain`, `text/markdown`, `application/json`, `application/yaml`, `application/x-yaml` und `text/yaml`. Dateinamen dürfen keine Pfadanteile oder Steuerzeichen enthalten. Archive (`.zip`, `.tar`, `.gz`, `.7z`, `.rar`) werden unabhängig vom MIME-Typ abgelehnt; ohne eine spätere isolierte Entpackprüfung gibt es keinen Archivpfad und somit keine Zip-Bomb-Gefahr.

I11.5 lädt keine Dateien von Zoho herunter und speichert keine Attachments. Ein ungültiger Anhang verwirft den gesamten Webhook fail-closed. Ein späterer Download muss dieselben Metadatengrenzen vor dem Abruf und zusätzliche Inhaltsprüfung nach dem Abruf einhalten.