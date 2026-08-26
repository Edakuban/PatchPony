# Ablauf-Cleanup von Sessions

I7.9 ergänzt `SessionExpiryCleanupService` für einen wiederkehrenden,
batchbegrenzten Worker-Lauf. Pro Ausführung werden höchstens 100 fällige,
nicht geschlossene Sessions über `ISessionRepository.GetDueForCleanupAsync`
geladen und in Ablaufreihenfolge verarbeitet.

Für eine aktive Session wird der Ablauf zuerst persistent nach `Expired`
festgehalten. Danach folgt der kontrollierte Übergang nach `Closing`, der
bestehende I7.8-Discard mit serverseitig aufgelöstem Base-Checkout und erst
nach Erfolg `Closed`. Auch nie vollständig aktivierte oder fehlgeschlagene,
abgelaufene Sessions können so bereinigt werden.

Ein Fehler beim Auflösen des Checkouts, beim Git-Discard oder beim lokalen
Cleanup schließt die Session nicht: Sie bleibt in `Closing` beziehungsweise
`Expired` und wird beim nächsten Lauf erneut berücksichtigt. Das Ergebnis
enthält nur Session-ID und stabilen Fehlercode, keine Pfade oder Git-Ausgaben.

Die Taktung und DI-Registrierung gehören zur Worker-Deployment-Konfiguration;
der Service besitzt keine Timer-, Netzwerk- oder Credential-Verantwortung.