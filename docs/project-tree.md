# Projektbaum

`ProjectTreeService` liefert eine read-only Übersicht der durch die
Manifest-Policy lesbaren Projektdateien und -verzeichnisse. Jeder Eintrag wird
zuerst kanonisch aufgelöst und autorisiert; absolute Host-Pfade sind kein Teil
des Ergebnisses.

Standardgrenzen sind fünf Ebenen Tiefe, 500 untersuchte Einträge und 1 MiB
Dateigröße für auszugebende Metadaten. Die Grenzen sind serverseitig
konfigurierbar, aber begrenzt. `IsTruncated` signalisiert eine erreichte
Tiefen- oder Eintragsgrenze; `HasOversizedEntries` signalisiert ausgelassene
große Dateien.

Directory-Symlinks werden nicht traversiert. Der Dienst liest keine
Dateiinhalte und führt keinerlei Repository-Inhalt aus.
