# Strukturierter Ticketplan

`TicketPlanOutput` entsteht nur für vollständige Tickets mit `ReadyForPlanning`. Er enthält Ticketreferenz, Projekt, Idempotenzschlüssel, bis zu 16 validierte Evidenzreferenzen und drei feste Planphasen:

1. freigegebene Projektquellen und Vorgaben prüfen,
2. einen überprüfbaren Änderungsvorschlag ausarbeiten,
3. den Vorschlag gegen registrierte Prüfungen und Reviewregeln validieren.

Evidenztypen sind `Skill`, `Source`, `Config` und `Knowledge`. Referenzen sind ausschließlich serverseitig validierte, relative Bezeichner; absolute Pfade, Backslashes, `..`, Steuerzeichen und rohe Inhalte werden abgelehnt. Der Plan enthält keine freien Toolargumente, keine Modellantwort und keine Schreib- oder Merge-Anweisung. I11.10 rendert ihn lesbar, I11.11 entscheidet erst danach anhand harter Policy über mögliche Automatisierung.