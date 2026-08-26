# Sandbox-Ergebnisse und Artefaktmetadaten

Nach Abschluss eines Sandbox-Prozesses schreibt der Worker ein Ergebnis als JSON in den serverseitig abgeleiteten Session-Pfad:

```text
<session-root>/sandbox-results/<execution-id>.json
```

Ein Ergebnis enthält die Execution-ID, Session-ID, den registrierten Command, Annahme- und Abschlusszeit, Ergebnisstatus (`Passed`, `Failed`, `TimedOut`), Exit-Code sowie die bereits begrenzten stdout/stderr-Daten und ihre Trunkierungsmerkmale.

Artefaktinhalte werden nicht in die Ergebnisdatei kopiert. Der Worker erfasst ausschließlich Metadaten aus dem festen Worktree-Unterordner `.patchpony-artifacts`: relativer Pfad, Bytegröße und SHA-256. Reparse-Points werden ausgeschlossen; maximal 100 reguläre Dateien von jeweils höchstens 100 MiB werden berücksichtigt. Beliebige Hostpfade und Artefaktpfade aus einem Auftrag sind nicht zulässig.

Schreibfehler werden aktuell fail-closed für die einzelne Ergebnisdatei behandelt; I9.11 ergänzt Cancellation, beobachtbare Fehlerbehandlung und hartes Cleanup.