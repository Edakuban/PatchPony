# Fehler- und Eskalationspfade für Tickets

`TicketWorkflowFailure` klassifiziert nur vier explizit normalisierte Infrastrukturfehler als wiederholbar: Git-Provider, Commit, Push und Zoho-Provider-Request. Jeder andere Fehler wird fail-closed als manuelle Untersuchung eingestuft.

Für Eskalationen erzeugt PatchPony ausschließlich `TicketEscalationRequest` mit Ticket-ID, Revision, Projekt, Idempotenzschlüssel und Fehlercode. Ticketbeschreibung, Anhänge, Secrets, Stacktraces und Providerantworten bleiben ausgeschlossen. Die Nutzerbotschaft ist fest und enthält keine technischen Details.

Die eigentliche Benachrichtigung bzw. Zoho-Kommentierung verwendet später diesen sicheren Datensatz; I11.14 selbst führt keine externe Aktion aus.