# Git-Publikationsvorlagen

Der Veröffentlichungsbranch wird ausschließlich aus der Session-ID mit der bestehenden Regel `patchpony/session/<session-id>` abgeleitet. Er ist nicht frei wählbar.

Die Commit-Message ist ebenfalls serverseitig fest:

```text
patchpony(<project-manifest-id>): apply validated session changes

PatchPony-Job: <job-id>
PatchPony-Session: <session-id>
```

Projekt-, Job- und Session-Zuordnung müssen übereinstimmen; Modelltext, Tickettext und beliebige Commit-Nachrichten fließen nicht in die Vorlage ein.