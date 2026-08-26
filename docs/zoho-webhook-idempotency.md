# Zoho-Webhook-Idempotenz

Für eine signaturgeprüfte Zoho-Aufgabe bildet PatchPony den Schlüssel ausschließlich aus `eventType`, `task.id` und `task.revision`:

```text
zoho:v1:<sha256>
```

Der Kanal, Titel, Beschreibung, Anhänge und die Projekt-ID sind absichtlich nicht Teil der Identität. Eine erneute Zustellung derselben Aufgabenversion bleibt damit identisch, während eine neue Revision zuverlässig einen neuen Schlüssel erhält.

Der Schlüssel wird nach I11.1 intern zusammen mit der bereits validierten Payload bereitgestellt, aber noch nicht an Clients ausgegeben und noch nicht persistent beansprucht. I11.3 und der spätere Job-Handoff verwenden ihn mit dem bestehenden Idempotenzrepository, sodass daraus keine doppelten Jobs, Kommentare oder Merge Requests entstehen.