# PostgreSQL backup and restore rehearsal

I14.4 provides a repeatable, isolated proof that the PostgreSQL dump and restore tooling can create a portable archive and reconstruct it in a fresh database. It is deliberately a rehearsal: it does **not** read `.env`, connect to the Compose `postgres` service, mount the `postgres-data` volume, or contain production credentials or data.

## Run the rehearsal

Run it on the machine and Docker context that will later operate PatchPony. For the Rootless deployment, point the Docker CLI at the dedicated application user's socket first.

```bash
export DOCKER_HOST=unix:///run/user/<app-user-uid>/docker.sock
bash deploy/host/postgres-backup-restore-test.sh
```

The script uses the same `postgres:18-alpine` image family as `docker-compose.yml` (or `PATCHPONY_POSTGRES_IMAGE` when an explicitly pinned replacement is needed). It then:

1. creates three uniquely named temporary Docker volumes and a temporary network;
2. starts a fresh source database containing only a deterministic probe row;
3. writes a PostgreSQL custom-format archive with `pg_dump`, verifies its SHA-256 and archive table of contents;
4. starts an entirely separate fresh target database and restores with `pg_restore --clean --if-exists --exit-on-error`;
5. verifies the probe row after restore; and
6. removes both containers, the network and all three explicitly named volumes through an exit trap, whether the run succeeds or fails.

A successful run ends with `PatchPony PostgreSQL backup/restore rehearsal: PASS`. The generated password is process-local test data and is never printed. Docker may pull the configured PostgreSQL image if it is not already present.

## Operational boundary

This is an executable restore-procedure test, not yet a production-data backup policy. Before the pilot holds important data, define separately: backup schedule and retention, encrypted off-host storage, access control, monitoring/alerting, and a controlled restore exercise from an actual protected snapshot. Do not repurpose the temporary rehearsal volumes or point the script at `postgres-data`.

PostgreSQL documents custom archives as a flexible format for `pg_restore` and recommends examining non-plain archives before restore; restoring a dump executes source-defined database objects, so restore only an archive from a trusted, approved backup source. See [pg_dump](https://www.postgresql.org/docs/current/app-pgdump.html) and [pg_restore](https://www.postgresql.org/docs/current/app-pgrestore.html).