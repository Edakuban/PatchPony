# RW-Pfadschutz für Sessions

I7.11 führt `SessionWorkspacePathGuard` als gemeinsame Schutzschicht für
schreibende Session-Operationen ein. Vor jeder Worktree-Anlage, Lease-Anlage,
Diff-Größenmessung und Discard-Operation wird geprüft, dass jeder bereits
existierende Pfadbestandteil zwischen dem dedizierten Session-Storage und dem
serverseitig abgeleiteten Ziel kein Reparse-Point/Symlink ist.

- Worktree-Anlage bricht vor dem Git-Aufruf mit `session.worktree.unsafe_path`
  ab.
- Diff und Größenmessung führen keinen Prozess aus und lesen keinen Worktree,
  wenn dessen Pfad auf einen Reparse-Point führt.
- Discard verweigert unsichere Wurzeln oder Worktrees. Die abschließende lokale
  Löschung traversiert den Sessionbaum selbst und stoppt bei jedem
  Reparse-Point, statt einer rekursiven Standardlöschung zu vertrauen.
- Der Lock-Storage selbst wird vor dem Anlegen atomarer Lease-Dateien geprüft.

Regressionstests erzeugen reale Directory-Symlinks für Sessionroot und
Worktree. Sie belegen, dass Anlage, Diff und Discard ohne Git-Aufruf abbrechen
und der außerhalb liegende Zielordner bestehen bleibt.

Die Prüfung reduziert den verbleibenden TOCTOU-Spielraum durch kurze
projekt-/branch-/pfadbezogene Leases. Der Session-Storage muss weiterhin ein
exklusiver, nicht von fremden Prozessen beschreibbarer Deployment-Pfad sein.