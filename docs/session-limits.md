# Session-Grenzen

I7.6 erzwingt zwei feste, serverseitige Grenzen für temporäre Arbeitskopien:

- Eine Session darf höchstens **8 Stunden** zwischen `CreatedAt` und `ExpiresAt`
  liegen. Die Prüfung ist Teil von `Session.Create`; damit gilt sie auch außerhalb
  einer API oder eines Workers. Eine Überschreitung liefert
  `session.duration.exceeded`.
- Ein neu angelegter Worktree darf höchstens **1 GiB** reguläre Dateidaten
  enthalten. Direkt nach dem kontrollierten `git worktree add` misst PatchPony
  den serverseitig abgeleiteten Pfad. Ein Überschreiten liefert
  `session.workspace_size_exceeded`.

Die Größenmessung verfolgt keine Reparse-Points/Symlinks und bricht früh ab, sobald
sie die Grenze erreicht. Ist der Worktree nicht messbar, wird die Anlage mit
`session.workspace_size_unavailable` konservativ abgelehnt. Ein abgewiesener
Worktree bleibt für die kontrollierten Discard- und Recovery-Schritte aus I7.8
beziehungsweise I7.10 erhalten.