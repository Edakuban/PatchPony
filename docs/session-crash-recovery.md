# Crash-Recovery für Session-Worktrees

I7.10 ergänzt `SessionCrashRecoveryService` für unvollständig beendete
Prozesse. Ein Recovery-Lauf lädt höchstens 100 persistierte Sessions im Zustand
`Provisioning` oder `Closing`, in dieser Reihenfolge nach ihrer letzten
Statusänderung.

- `Provisioning` wird als nicht erfolgreich abgeschlossene Anlage behandelt und
  kontrolliert nach `Closing` überführt.
- `Closing` wird direkt wiederaufgenommen.
- Beide Wege verwenden danach denselben I7.8-Discard mit Lease-Locks,
  serverseitigem Checkout, festem Git-Kommando und abgeleitetem Pfad/Branch.
- Erst nach erfolgreichem Discard wird `Closed` persistiert. Fehler verbleiben
  in `Closing` und sind beim nächsten Lauf erneut auswählbar.

Der Recovery-Service löscht **keine unbekannten Dateisystemordner**: Ohne
passenden Session-Datensatz fehlen Projekt- und Checkout-Autorisierung. Solche
Fälle bleiben für operative Prüfung erhalten. Die weitergehende Absicherung
gegen Symlink- und Worktree-Ausbrüche folgt in I7.11.