# Session-Status und Änderungen

I7.7 stellt zwei interne, ausschließlich lesende Dienste bereit:

- `SessionApplicationService.DescribeAsync` lädt den persistierten Sessionzustand
  inklusive Projekt, Job, Ablaufzeit, Lifecycle-Status und gegebenenfalls
  Fehlercode. Eine unbekannte Session liefert `session.not_found`.
- `GitSessionDiffService.GetAsync` liefert den Arbeitsstand einer `Active`- oder
  `Closing`-Session als begrenzten Unified Diff. Worktree-Pfad und Branch werden
  ausschließlich aus der serverseitigen Session-ID abgeleitet.

Der Diff-Aufruf ist fest auf
`git -C <server-worktree> diff --no-ext-diff --no-color --no-textconv --no-renames --`
begrenzt. Er akzeptiert weder Pfade noch freie Git-Argumente; externe Diff- und
Textconv-Helfer sind deaktiviert. Prozessausgabe ist auf 64 KiB begrenzt. Fehler
sind stabil als `session.diff.invalid_state`, `session.worktree_unavailable`,
`session.diff.failed` oder `session.diff.timeout` verfügbar.

Die Dienste besitzen noch keinen externen REST- oder MCP-Transport, weil die
Session-Provisionierung selbst erst mit den folgenden I7-Paketen als vollständiger
Lifecycle bereitsteht. Ein Transport darf ausschließlich diese serverseitig
abgeleiteten Operationen exponieren.