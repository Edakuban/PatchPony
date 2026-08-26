# Strukturierte Änderungsaufträge

I12.1 trennt die fachliche Änderungsabsicht von Planung, Validierung und Ausführung. Nur Zoho-Tasks mit `channel` `feature_request` oder `change_request` dürfen einen `TicketChangeRequest` erzeugen. Ein Auftrag enthält ausschließlich:

- `Configuration`, `SourceCode`, `DatabaseMigration` oder `Secret` als Änderungsart,
- ein bis sechzehn eindeutige, relative Zielreferenzen,
- optional einen begrenzten Sollwert je Ziel.

Zielreferenzen sind maximal 200 Zeichen lang, dürfen weder mit `/` beginnen noch Backslashes oder `..` enthalten und transportieren keine freie Befehlszeile. Sollwerte bleiben auf 512 Zeichen und reine Textdaten begrenzt. Für die Änderungsart `Secret` ist ein Sollwert bereits im Modell verboten.

Der Vertrag nimmt keine Umgebung, keine Freigabe, keine Toolargumente und keine Ausführungsentscheidung an. Diese Grenzen folgen erst mit I12.2 bis I12.7. `ZohoTicketChangeRequestService` ist ausschließlich eine Gateway-Fassade für den späteren kontrollierten n8n-Handoff; I12.1 fügt keinen öffentlichen Endpoint hinzu.