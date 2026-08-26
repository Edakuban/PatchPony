# Retention automation

I14.7 automates bounded retention for PostgreSQL operational data and container logs. It never reads `.env`, deletes source checkouts, worktrees, backups, vault content, Docker images/volumes, or protected secret files.

## Retention policy

| Data | Retention | Safety gate |
| --- | --- | --- |
| Audit events | 180 days | oldest-first, at most 1,000 records per batch |
| Completed tool invocations | 90 days | only rows with `completed_at` |
| Idempotency records | expiry time | expiry is already domain-defined |
| Sessions | 90 days after `Closed` | active, failed, expired and closing sessions are excluded |
| Terminal jobs | 180 days | only Closed/Failed/Cancelled jobs without retained audit references |
| Docker JSON logs | 30 MiB per container | 10 MiB × 3 rotated files via Compose |
| Prometheus samples | 30 days | configured in the private monitoring stack |

Run a dry-run first, only after a successful I14.4 backup rehearsal and review of its aggregate counts:

```bash
export PATCHPONY_RETENTION_COMPOSE_DIRECTORY=/srv/patchpony
bash deploy/host/retention.sh dry-run
```

Apply requires the exact confirmation and is bounded to 500 records × 10 batches by default:

```bash
export PATCHPONY_RETENTION_CONFIRM=DELETE_ONLY_APPROVED_PATCHPONY_RETENTION
bash deploy/host/retention.sh apply
```

For unattended operation, copy the versioned user units to `~/.config/systemd/user/`, ensure the protected `runtime.env` is mode 0600, then enable the timer. The service deliberately runs as the Rootless application user and uses its Docker socket; it has no root, host-path, or network-execution permission.

```bash
systemctl --user daemon-reload
systemctl --user enable --now patchpony-retention.timer
systemctl --user list-timers patchpony-retention.timer
```

Review the first dry-run and first automated result in the private monitoring dashboard. Change retention windows only through reviewed configuration and repeat the backup/restore test before lowering them.