# Strukturiertes Ticket-Triage-Ergebnis

`TicketTriageOutput` ist der transportneutrale Übergabevertrag nach Webhook-Validierung, Projektzuordnung und Vollständigkeitsprüfung. Er enthält ausschließlich:

- externe Ticket-ID und Revision
- Manifest-Projekt-ID und Eventtyp
- versionierten Idempotenzschlüssel
- Vollständigkeitsstatus und stabile Missing-Criteria-IDs
- deterministische Disposition `NeedsInformation` oder `ReadyForPlanning`

Beschreibung, Titel, Anhänge, Provider-Signatur, Prompt und Modellantwort sind ausdrücklich nicht Teil dieses Ergebnisses. `NeedsInformation` folgt ausschließlich aus fehlenden serverkonfigurierten Kriterien; `ReadyForPlanning` ist keine Freigabe für Änderungen, Merge Requests oder Modellaufrufe.