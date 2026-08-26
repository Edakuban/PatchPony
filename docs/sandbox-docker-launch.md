# Docker-Sandbox-Start

`DockerSandboxContainerLauncher` startet ausschließlich den aus den serverseitigen Runner- und Command-Katalogen abgeleiteten Aufruf:

```text
docker run --rm --name <server-execution-name> --user 65532:65532 --read-only --mount type=bind,src=<server-session-worktree>,dst=/workspace,rw,bind-propagation=rprivate --workdir /workspace --cpus <server-cpu> --memory <server-bytes> --pids-limit <server-pids> --storage-opt size=<server-bytes> --network none --cap-drop=ALL --security-opt no-new-privileges:true --security-opt seccomp=<server-profile> --security-opt apparmor=<server-profile> <digest-pinned-image> <registered-executable> <registered-arguments>
```

Damit läuft der Container nicht als Root, sein Root-Dateisystem ist schreibgeschützt und alle Linux-Capabilities werden entfernt. Weder Image, Executable noch Argumente stammen aus dem Auftrag.

Der Launcher ist ausschließlich über `ISandboxWorkerApi` im Worker registriert; Gateway und öffentliche APIs haben keinen Docker-Zugriff. Ressourcenlimits und der beschreibbare Session-Mount sind ausschließlich serverseitige Worker-Konfiguration. Details stehen in [sandbox-resource-limits.md](sandbox-resource-limits.md), [sandbox-worktree-mount.md](sandbox-worktree-mount.md), [sandbox-output-limits.md](sandbox-output-limits.md) [sandbox-cancellation-cleanup.md](sandbox-cancellation-cleanup.md) und [sandbox-rootless-docker.md](sandbox-rootless-docker.md).