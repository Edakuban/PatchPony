# Pilot monitoring and alerts

I14.6 provisions a private Prometheus/Grafana monitoring stack for the PatchPony pilot. Grafana binds only to `127.0.0.1:13000`; it is not routed through Caddy and must be accessed through an SSH tunnel or local console. Prometheus and the PostgreSQL exporter have no published ports. The exporter is the only observability service on the existing internal PatchPony data network.

## Signals

The versioned dashboard contains queue depth and age, failed jobs over 15 minutes, active sessions, expired non-terminal sessions, and PostgreSQL database size. `postgres-queries.yaml` derives the workflow indicators directly from the existing `patchpony` schema; it contains no job text, paths, correlation IDs or secrets.

Alert rules cover exporter availability, a queue older than 15 minutes, recent job failures, expired unresolved sessions, and a 10 GiB initial database-growth guardrail. Configure Alertmanager or the chosen private notification receiver separately before treating alerts as operationally delivered.

## Deployment

The Rootless application operator supplies the standard protected runtime environment plus a separate Grafana password. First determine the existing Compose data-network name, then start the private stack:

```bash
export DOCKER_HOST=unix:///run/user/<app-user-uid>/docker.sock
export PATCHPONY_OBSERVABILITY_DATA_NETWORK=<existing-patchpony-data-network>
export GRAFANA_ADMIN_PASSWORD=<secret-from-secret-manager>
docker compose --env-file /protected/path/patchpony-runtime.env \
  -f deploy/observability/docker-compose.yml up -d
```

Open Grafana only through `http://127.0.0.1:13000` locally or a controlled SSH tunnel. Confirm the provisioned **PatchPony / PatchPony Pilot Operations** dashboard and verify that each Prometheus target is up. The dashboard is declarative and read-only in Git; do not store alert receiver credentials in this repository.