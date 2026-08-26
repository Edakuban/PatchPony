# Incident, recovery and kill-switch runbooks

## First response

1. Open an incident record with time, reporter, affected project and a redacted symptom. Do not paste tokens, ticket text, source, model prompts or raw logs.
2. Preserve current aggregate monitoring state and relevant correlation IDs. Do not delete data or restart services before deciding whether containment is needed.
3. For suspected unauthorized access, credential leakage, unexpected worker execution, injection bypass or active data exfiltration, use the kill switch immediately.

## Kill switch

The switch stops only the Rootless Gateway and private sandbox Worker. PostgreSQL, named volumes, backups, source directories, firewall and Caddy configuration are untouched. n8n/Open WebUI then receive unavailable responses and no new PatchPony work can run.

```bash
export PATCHPONY_KILLSWITCH_COMPOSE_DIRECTORY=/srv/patchpony
export PATCHPONY_KILLSWITCH_RUNTIME_ENV=/protected/path/patchpony-runtime.env
export PATCHPONY_KILLSWITCH_CONFIRM=DISABLE_PATCHPONY_EXECUTION
bash deploy/host/kill-switch.sh disable
bash deploy/host/kill-switch.sh status
```

Do not re-enable merely because the alert stops. First identify scope, preserve redacted evidence, rotate implicated credentials with [credential rotation](credential-rotation.md), review audit signals, and decide whether a restore is necessary. Two people (incident lead and technical reviewer) must approve resumption:

```bash
export PATCHPONY_KILLSWITCH_CONFIRM=ENABLE_PATCHPONY_AFTER_INCIDENT_REVIEW
bash deploy/host/kill-switch.sh enable
```

## Recovery paths

- **Credential suspicion:** keep the switch active; follow the complete I14.5 rotation window, including n8n, Zoho, Open WebUI and GitHub where enabled.
- **Gateway/Worker fault:** retain the database, inspect only redacted service status/logs, correct the reviewed deployment, then use `enable` and confirm readiness/worker state.
- **Data integrity or accidental deletion:** stop execution, preserve the current database, then use the tested restore procedure in [postgresql-backup-restore.md](postgresql-backup-restore.md). A restore requires an approved protected snapshot and a separate change record.
- **Host compromise:** disconnect the host through the organization’s infrastructure process; do not assume this script is sufficient. Rebuild from the Debian baseline, re-establish Rootless Docker/firewall, restore only approved data, and rotate every credential.

## Closure

Before closing: verify health, worker state, authentication, a controlled n8n request, monitoring/alert delivery, and that no unreviewed branch or merge was created. Record timeline, impact, correlation IDs, remediation, credential references, recovery result and follow-up owner. Feed new threat details into [threat-model.md](threat-model.md).