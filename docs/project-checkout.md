# Kontrollierter Base-Checkout

`GitBaseCheckoutService` erzeugt und aktualisiert den Base-Checkout eines
registrierten Projekts. Er akzeptiert ausschließlich die persistierte
Repository-URL und den persistierten Default-Branch; beide werden nicht aus
Tool- oder Agenteneingaben übernommen.

Für ein neues Projekt klont der Dienst ohne initialen Checkout und lädt danach
nur den angegebenen Branch. Für einen bestehenden Checkout prüft er zuerst die
`origin`-URL exakt gegen die Registrierung. Bei Abweichung bricht er ab, bevor
ein Fetch erfolgt.

Git wird ausschließlich über einen serverseitig festgelegten Executable-Namen
und `ProcessStartInfo.ArgumentList` gestartet. Shell-Interpreter werden nicht
verwendet. Interaktive Prompts, System-Git-Konfiguration und unbegrenzte
Ausgaben sind deaktiviert beziehungsweise begrenzt; jeder Aufruf hat einen
Timeout. Der Checkout-Pfad wird allein aus einem serverseitigen Storage-Root
und der internen Projekt-GUID gebildet.

Der Dienst ist eine Infrastrukturgrenze. Seine Verzeichnisse sind nicht für
Agenten schreibbar; kanonische Pfadauflösung und Symlink-Schutz sind vorhanden; die Pfad-Policy folgt in I3.8.
## Revisionsbindung

Nach dem Detached Checkout löst der Dienst `HEAD^{commit}` auf. Nur eine
vollständige SHA-1- oder SHA-256-Commit-ID wird als `RepositoryRevision`
angenommen und zusammen mit dem `BaseCheckout` zurückgegeben. Nachfolgende
Leseoperationen erhalten damit eine unveränderliche Referenz; ein späterer
Fetch desselben Branches kann diese bereits zurückgegebene Revision nicht
verändern.