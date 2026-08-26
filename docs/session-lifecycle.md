# Session-Lifecycle

I7.1 führt einen expliziten, serverseitig validierten Lifecycle für isolierte Sessions ein. Eine neue Session startet in `Created`; ihr Zustand und der Zeitpunkt der letzten Änderung werden persistent gespeichert.

```text
Created → Provisioning → Active → Closing → Closed
                   ↘ Failed ←───────┘
Active → Expired → Closing → Closed
Failed → Closing
Provisioning → Closing
Created → Closing
```

- `Expired` ist erst ab `ExpiresAt` zulässig.
- `ExpiresAt` darf höchstens acht Stunden nach `CreatedAt` liegen; Details stehen in [session-limits.md](session-limits.md).
- `Closed`, `Expired` und `Failed` können nicht aktiviert werden; `Expired` und `Failed` dürfen ausschließlich für einen kontrollierten Cleanup nach `Closing` wechseln.
- Übergänge dürfen zeitlich nicht rückwärts laufen.
- `Failed` benötigt einen kurzen, serverseitig validierten Fehlercode. Rohfehler, Hostpfade oder Secrets sind kein zulässiger Fehlercode.
- Wiederholte oder nicht erlaubte Übergänge liefern stabile Fehlercodes: `session.transition.noop` beziehungsweise `session.transition.invalid`.

Die Migration `20260823090000_AddSessionLifecycle` ergänzt Status, Statuszeitpunkt, Fehlercode und einen Ablaufindex. I7.2 ergänzt darauf das serverseitige Verzeichnislayout; erst I7.3 legt tatsächlich Git-Worktrees an.