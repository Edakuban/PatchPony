# Serverseitige Session- und Branchnamen

I7.4 erzeugt beide Namen ausschließlich aus der bereits serverseitig erzeugten `SessionId`:

| Zweck | Format |
|---|---|
| Sessionverzeichnis | `<guid-ohne-trennzeichen>` |
| Git-Branch | `patchpony/session/<guid-ohne-trennzeichen>` |

`SessionNaming` hat keine Parameter für Präfix, Projektnamen, Tickettext, Benutzername oder Modelloutput. Das Verzeichnislayout und die Worktree-Anlage verwenden diese gemeinsame Core-Policy. Dadurch kann kein Agent einen Branch in einen geschützten Namensraum lenken oder einen Pfadbestandteil bestimmen.

Die Branch-Kollisions- und Lock-Behandlung folgt in I7.5; der Branchname selbst bleibt trotzdem deterministisch und überprüfbar.