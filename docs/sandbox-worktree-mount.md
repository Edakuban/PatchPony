# Schreibbarer Session-Worktree

Jeder Sandbox-Start erhält genau einen schreibbaren Bind-Mount:

```text
--mount type=bind,src=<server-derived-session-worktree>,dst=/workspace,rw,bind-propagation=rprivate --workdir /workspace
```

`<server-derived-session-worktree>` wird aus `PatchPony:Worker:SandboxWorkspace:StorageRoot` und der serverseitig geprüften Session-ID durch den bestehenden Session-Layout-Resolver abgeleitet. Der Worker startet keinen Container, wenn der Worktree nicht existiert, außerhalb des Storage-Roots liegt oder ein Reparse-Point/Symlink im Pfad vorkommt.

Der Auftrag übergibt weder Host- noch Containerpfade. Außer `/workspace` gibt es keinen schreibbaren Host-Mount; Docker-Socket, Credentials, Repository-Basischeckout und das übrige Host-Dateisystem werden nicht eingebunden. Das Container-Root-Dateisystem bleibt durch `--read-only` geschützt.

Beispielkonfiguration:

```json
{
  "PatchPony": {
    "Worker": {
      "SandboxWorkspace": {
        "StorageRoot": "/var/lib/patchpony"
      }
    }
  }
}
```