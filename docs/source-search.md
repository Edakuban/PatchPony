# Source-Suche

`RipgrepSourceSearchService` sucht nur in zuvor kanonisch aufgelösten und
durch die Manifest-Policy freigegebenen Dateien. Der Prozess startet das
serverseitig festgelegte `rg` direkt über `ProcessStartInfo.ArgumentList`;
Shell-Interpreter und frei wählbare Tooloptionen sind ausgeschlossen.

Die feste Suchkonfiguration verwendet JSON-Ausgabe, Literal-Suche,
Zeilennummern, maximal 20 Treffer pro Datei, maximal 200 untersuchte Dateien,
maximal 100 zurückgegebene Treffer, 1 MiB pro Kandidatendatei, 4 KiB pro
Ausgabezeile, 256 KiB Gesamtausgabe und einen Timeout von zehn Sekunden.

`IsTruncated` signalisiert erreichte Datei-, Treffer- oder Ausgabegrenzen.
Dateipfade im Ergebnis sind ausschließlich relative Projektpfade. Die Suche materialisiert keine vollständigen Quelldateien in PatchPony und führt keinen Repository-Inhalt aus.
