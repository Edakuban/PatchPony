# Rootless-Docker-Prüfung

Der Worker verlangt standardmäßig einen Rootless-Docker-Daemon. Beim Start führt er ohne Shell und mit festem Timeout aus:

```text
docker info --format {{json .SecurityOptions}}
```

Nur wenn die Security-Options `name=rootless` enthalten, startet der Worker. Das Verhalten ist über `PatchPony:Worker:RootlessDocker:Required` steuerbar, bleibt aber standardmäßig verpflichtend. Ein nicht erreichbarer oder nicht-rootless Docker-Daemon verhindert den Worker-Start fail-closed.

Jeder Runner benötigt außerdem die explizite Registrierung `RootlessCompatible: true`. Sie bestätigt die Kompatibilität mit dem festen Containerbenutzer `65532:65532`, `--read-only`, `cap-drop=ALL`, deaktiviertem Netzwerk und dem einzigen Worktree-Mount. Die tatsächliche Prüfung eines neuen Runner-Images erfolgt vor seiner Registrierung durch den Betreiber; unbestätigte Images werden vom Katalog abgewiesen.

Hinweis: Rootless Docker und bind-mount-basierte Worktrees benötigen einen Linux-Deployment-Host mit passenden Besitzrechten für den Container-UID/GID-Mapping. Die lokale Windows-Entwicklung sollte über die Linux-VM von Docker Desktop bzw. eine äquivalente Rootless-Linux-Umgebung erfolgen.
## Production deployment

Production uses a dedicated non-root `patchpony-sandbox` account and Rootless Docker daemon rather than a Docker group membership or a rootful socket. The preparation, user-session activation, cgroup-v2 verification and private Worker systemd unit are documented in [rootless-docker-production.md](rootless-docker-production.md). The all-in-one Compose Worker remains a local-development artifact and is not the production sandbox deployment.
