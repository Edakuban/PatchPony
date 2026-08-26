# Gezielte Ticket-Rückfragen

Wenn die Triage `NeedsInformation` liefert, erzeugt PatchPony einen `TicketInformationRequest` aus den serverseitigen Missing-Criteria-IDs. Der Entwurf enthält Ticketreferenz, Projekt, Idempotenzschlüssel und höchstens fünf feste deutsche Fragen.

Bekannte Kriterien werden deterministisch gerendert: fehlende Detailtiefe fordert Reproduktionsschritte an, fehlende Anhänge zulässige Diagnoseanhänge, und konfigurierbare Textmerkmale eine ergänzende Angabe. Unbekannte Kriterien werden abgelehnt, nicht frei formuliert. Ticketbeschreibung und Anhänge werden nicht erneut in den Entwurf kopiert.

I11.8 sendet keinen Zoho-Kommentar. Die kontrollierte, idempotente Zustellung eines solchen Entwurfs folgt erst in I11.13.