# Production network boundary

I14.3 exposes exactly one public edge: the host-native Caddy service on TCP 80 and 443. The Rootless application stack publishes the Gateway only on `127.0.0.1:18080`; PostgreSQL has no published port and uses the internal Compose `data` network. The local Caddy and Worker Compose services are disabled by the Rootless production overlay because production uses the host Caddy and the separate private sandbox-worker user unit from I14.2.

## Host firewall

[`deploy/host/nftables-edge-firewall.sh`](../deploy/host/nftables-edge-firewall.sh) renders and validates a default-drop `inet` ruleset. It allows established traffic, loopback, ICMP/ICMPv6, the already configured SSH port, and TCP 80/443 only. Forwarding is default-drop; outbound traffic is left available for package updates, model-provider access and approved external integrations.

Before applying, keep a local console or a second verified SSH-key session open:

```bash
export PATCHPONY_SSH_PORT=<effective-sshd-port>
export PATCHPONY_CONFIRM_FIREWALL=I_HAVE_A_SECOND_KEY_SESSION
sudo -E bash deploy/host/nftables-edge-firewall.sh apply
sudo -E bash deploy/host/nftables-edge-firewall.sh audit
```

The script validates the candidate before loading it, saves the prior `/etc/nftables.conf` as a local backup, and verifies the active policy. It does not open a Docker port, expose PostgreSQL, or manage a cloud security group.

## Rootless application stack and host Caddy

Copy [`deploy/caddy/Caddyfile.host.production`](../deploy/caddy/Caddyfile.host.production) into the host Caddy configuration after setting the protected service environment variable `PATCHPONY_PUBLIC_HOST`. Validate and reload Caddy using the operator's installed Caddy package. Then launch only the Rootless application services:

```bash
export PATCHPONY_PUBLIC_HOST=patchpony.example.com
export DOCKER_HOST=unix:///run/user/<app-user-uid>/docker.sock
docker compose -f docker-compose.yml -f deploy/production/docker-compose.rootless.yml up --build -d
```

Verify locally on the host that the Gateway binds only to `127.0.0.1:18080`, then verify externally through the public HTTPS hostname. Do not use `deploy/caddy/docker-compose.production.yml` for this Rootless deployment: rootless containers cannot bind privileged ports by default, and the native Caddy edge avoids granting that capability.

Debian documents `nftables` as its default firewall framework and persistent service configuration. Docker documents that ports published with `127.0.0.1` are host-local, whereas unspecified published ports are externally reachable; Rootless Docker cannot bind ports below 1024 by default. See [Debian nftables](https://wiki.debian.org/nftables), [Docker port publishing](https://docs.docker.com/engine/network/port-publishing/), and [Docker Rootless troubleshooting](https://docs.docker.com/engine/security/rootless/troubleshoot/).