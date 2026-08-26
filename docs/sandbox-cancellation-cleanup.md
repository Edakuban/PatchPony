# Sandbox-Abbruch und hartes Cleanup

Jeder Sandbox-Lauf erhält vor dem Docker-Start eine serverseitige Execution-ID und den Containernamen `patchpony-sandbox-<execution-id>`. Dieser Name stammt nie aus dem Auftrag.

Bei Cancellation des Worker-Auftrags wird der Docker-Client-Prozessbaum beendet und anschließend `docker rm --force <server-container-name>` ausgelöst. Der Watchdog nutzt denselben Cleanup-Pfad nach Ablauf des Ausführungszeitlimits. Docker startet weiterhin mit `--rm`; das explizite Entfernen behandelt den Fall, dass der Client gewaltsam beendet wird, bevor Docker den Container selbst bereinigt.

Die Cancellation-Registrierung wird nach Abschluss der stdout/stderr-Erfassung freigegeben. Bei der Ergebnisablage werden temporäre JSON-Dateien im `finally` entfernt, falls Schreiben oder Umbenennen scheitert. Alle Cleanup-Aufrufe verwenden ausschließlich serverseitig erzeugte Werte und führen keinen Shell-Befehl aus.