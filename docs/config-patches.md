# Kontrollierte Config-Patches

`ConfigPatchService` wendet ausschließlich vollständige Ersetzungen bereits vorhandener JSON-, YAML- oder XML-Dateien in einem serverseitig aufgelösten Session-Worktree an.

Ein `ConfigPatchRequest` enthält den relativen Zielpfad, den erwarteten Dateityp, den SHA-256-Hash der Ausgangsdatei und die neue UTF-8-Nutzlast. Der Request wird abgelehnt, wenn:

- der Pfad aus dem Worktree herausführt oder durch einen Link unsicher wird,
- die Zieldatei fehlt oder selbst ein Reparse Point ist,
- Dateiendung und erwartetes Format nicht übereinstimmen,
- der erwartete Hash nicht exakt der aktuellen Ausgangsdatei entspricht,
- die Ausgangsdatei oder Ersetzung das Größenlimit (standardmäßig 1 MiB) übersteigt.

Die neue Datei wird als zufällig benannte temporäre Datei im Zielverzeichnis geschrieben und anschließend atomar über die ursprüngliche Datei verschoben. Ein fehlgeschlagener oder veralteter Patch verändert die Quelle nicht.

Zusätzlich erzwingt der Service die projektbezogene Write-Policy. Ein Ziel muss in `paths.writable` liegen; ein Treffer in `paths.forbidden` wird immer abgewiesen.
## Diff-Vorschau

`ConfigPatchDiffService` erzeugt vor einer Änderung eine Unified-Diff-Vorschau nur für denselben hash-gepinnten, im Manifest schreibbaren Patchauftrag. Die Vorschau schreibt nichts in den Worktree.

Vor der Ausgabe werden Werte von Secret-ähnlichen Schlüsseln (unter anderem `password`, `secret`, `token`, `apiKey` und `authorization`) für YAML-, JSON- und XML-Notation durch `[REDACTED]` ersetzt. Die Ausgabe ist zusätzlich standardmäßig auf 200 geänderte Zeilen und 64 KiB UTF-8 begrenzt. Erreicht die Vorschau eines der Limits, kennzeichnet `IsTruncated` die unvollständige Darstellung.
## Validierung und Rücknahme

`ValidatedConfigPatchService` hält den Ausgangsinhalt vor dem Schreiben fest und validiert die exakt geschriebene Nutzlast unmittelbar über den serverseitig zusammengesetzten `ConfigAdapterCatalog` — einschließlich eines optionalen, serverseitig registrierten `SchemaId`.

Liefert der Parser oder das Schema einen ungültigen Report, wird die Ausgangsdatei atomar wiederhergestellt. Der Rollback prüft zuvor den Hash des gerade geschriebenen Ergebnisses. Wurde die Datei inzwischen erneut verändert, wird mit `patch.rollback_conflict` abgebrochen und diese neuere Änderung bleibt unangetastet. Kann die Validierung nicht ausgeführt werden, wird der Patch ebenfalls zurückgenommen, bevor ein Fehler zurückgegeben wird.