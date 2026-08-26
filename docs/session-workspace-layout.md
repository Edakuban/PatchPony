# Session-Verzeichnislayout

I7.2 reserviert für jede Session einen ausschließlich serverseitig abgeleiteten Pfad:

```text
<SESSION_STORAGE_ROOT>/
  sessions/
    <session-id-als-guid-ohne-trennzeichen>/
      worktree/       # erst ab I7.3 angelegt
      session.json    # Metadaten, erst bei kontrollierter Anlage
      .disabled-hooks/ # leerer, serverseitiger Git-Hook-Pfad
```

`SessionWorkspaceLayoutResolver` akzeptiert nur einen absoluten, serverseitigen Storage-Root und bildet die Session-ID im Format `N` ab. Es gibt keinen API-Parameter für Basis-, Session-, Worktree-, Metadaten- oder Lockpfade. Der Resolver erzeugt weder Verzeichnisse noch Dateien und führt keine Git-Operation aus. Seit I7.5 liegen die aktiven, projektübergreifenden Lease-Locks zentral unter `<SESSION_STORAGE_ROOT>/locks`; ihre Namen werden ebenso ausschließlich serverseitig abgeleitet.

Der Storage-Root wird erst mit I7.3 als Deployment-Konfiguration gesetzt. Er muss ein dedizierter Datenpfad sein, nie der Repository-, Home-, Docker-Socket- oder Credential-Pfad. Worktrees erhalten später keinen Host- oder Git-Credential-Mount.