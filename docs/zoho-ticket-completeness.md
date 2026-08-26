# Projektspezifische Ticketvollständigkeit

Für jede gemappte `ProjectManifestId` ist unter `PatchPony__Zoho__CompletenessPolicies` genau eine Policy erforderlich. Sie definiert:

- `MinimumDescriptionLength` von 1 bis 16 KiB
- `MinimumAttachmentCount` von 0 bis 5
- bis zu fünf `RequiredDescriptionPhrases`

Die Auswertung liefert nur stabile Kriterien-IDs wie `description.minimum_length`, `attachments.minimum_count` und `description.required_phrase:<phrase>`; sie gibt keinen Ticketinhalt zurück. Fehlende oder ungültige Projektpolicy ist ein fail-closed Konfigurationsfehler (`503`). Ein unvollständiges, ansonsten gültiges Ticket bleibt dagegen angenommen: I11.8 verwendet das Ergebnis später für eine gezielte Rückfrage statt Informationen zu erfinden.