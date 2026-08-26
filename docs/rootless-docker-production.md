# Rootless Docker for the production sandbox worker

I14.2 selects a dedicated, non-root `patchpony-sandbox` operating-system account and its own Rootless Docker daemon for sandbox execution. The account must not belong to the `docker` group, and the host must not retain an active rootful Docker daemon. The Worker is the only PatchPony component given this Rootless socket; Gateway, n8n and Open WebUI never receive it.

## Two phases

Run the root-owned preparation on the Debian 13 host once. The subordinate range must be allocated by the host operator and must not overlap any existing range:

```bash
export PATCHPONY_DOCKER_USER=patchpony-sandbox
export PATCHPONY_SUBID_START=<approved-free-range-start>
sudo -E bash deploy/host/rootless-docker.sh prepare
```

This installs Rootless prerequisites, checks cgroup v2, creates the user-specific subordinate UID/GID range, delegates the required cgroup controllers only to that user's systemd instance, and enables lingering. It intentionally refuses to run while a rootful Docker service or socket is active.

Install Docker Engine and `docker-ce-rootless-extras` from the approved package source, then log in as `patchpony-sandbox` through a real PAM/systemd session (not `sudo su`) and activate:

```bash
export PATCHPONY_DOCKER_USER=patchpony-sandbox
bash deploy/host/rootless-docker.sh activate
bash deploy/host/rootless-docker.sh audit
```

The audit requires an active user `docker.service`, `/run/user/<uid>/docker.sock`, the `rootless` Docker context and security option, the systemd cgroup driver, cgroup v2, and sufficient subordinate IDs. This is important because the Worker relies on Docker CPU, memory and PID limits; a daemon without the systemd cgroup driver is rejected.

## Private Worker service

[`deploy/systemd/patchpony-sandbox-worker.service`](../deploy/systemd/patchpony-sandbox-worker.service) is a **user** systemd unit for this account. It runs the Worker as that non-root UID/GID, with a read-only root filesystem, dropped capabilities, no container network, only the Rootless Docker socket, and the server-owned session-storage mount. Its protected `~/.config/patchpony/worker.env` must be mode `0600`, outside Git, and contain:

```text
PATCHPONY_WORKER_IMAGE=<digest-pinned-worker-image>
PATCHPONY_SESSION_STORAGE_ROOT=<absolute-dedicated-session-storage-root>
PATCHPONY__WORKER__ID=<server-owned-id>
PATCHPONY__WORKER__CLAIMSIGNINGKEY=<secret>
```

The unit must be copied by the operator to `~/.config/systemd/user/patchpony-sandbox-worker.service`, followed by `systemctl --user daemon-reload` and `systemctl --user enable --now patchpony-sandbox-worker`. Do not use the current all-in-one Compose stack as the production worker deployment: it does not bind a Rootless socket and its public Caddy-port topology is finalized only in I14.3.

Docker documents that Rootless mode needs `uidmap` plus subordinate UID/GID ranges, is installed by `dockerd-rootless-setuptool.sh` as a non-root user, and uses a user systemd service with lingering for boot persistence. Docker also documents the cgroup-v2/systemd requirement for container resource flags. See [Rootless mode](https://docs.docker.com/engine/security/rootless/) and [Rootless tips](https://docs.docker.com/engine/security/rootless/tips/).