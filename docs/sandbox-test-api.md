# Gateway-Testoberfläche

Die Testoberfläche wird unter `PatchPony:TestSessions` serverseitig konfiguriert. Eine Session enthält Projekt-ID, Session-ID, Worktree-Pfad und die geschlossene Menge erlaubter Command-IDs.

`tests.run` validiert den Command gegen diese Menge und schreibt ausschließlich eine serverseitig erzeugte Request-Datei nach `<session-root>/sandbox-requests/`. Der Gateway hat keinen Docker-Zugriff. Der private Worker verarbeitet den Handoff und schreibt das Ergebnis nach `<session-root>/sandbox-results/`; `tests.result` liest nur diese gespeicherte Datei.

Beispiel:

```json
{
  "PatchPony": {
    "TestSessions": {
      "Sessions": [{
        "ProjectId": "patchpony",
        "SessionId": "0123456789abcdef0123456789abcdef",
        "WorktreeRoot": "/var/lib/patchpony/sessions/pp-session-0123456789abcdef0123456789abcdef/worktree",
        "Commands": ["test.billing.unit"]
      }]
    }
  }
}
```