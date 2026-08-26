#!/usr/bin/env bash
# Bounded PostgreSQL retention job. Default is read-only; apply requires explicit confirmation.
set -Eeuo pipefail
MODE="${1:-dry-run}"
COMPOSE_DIRECTORY="${PATCHPONY_RETENTION_COMPOSE_DIRECTORY:-}"
BATCH_SIZE="${PATCHPONY_RETENTION_BATCH_SIZE:-500}"
MAX_BATCHES="${PATCHPONY_RETENTION_MAX_BATCHES:-10}"
CONFIRM="${PATCHPONY_RETENTION_CONFIRM:-}"

fail() { printf '%s\n' "ERROR: $*" >&2; exit 1; }
[[ -n "${COMPOSE_DIRECTORY}" && -d "${COMPOSE_DIRECTORY}" ]] || fail "set PATCHPONY_RETENTION_COMPOSE_DIRECTORY to the protected deployment checkout"
[[ "${BATCH_SIZE}" =~ ^[0-9]+$ && "${BATCH_SIZE}" -ge 1 && "${BATCH_SIZE}" -le 1000 ]] || fail "PATCHPONY_RETENTION_BATCH_SIZE must be 1..1000"
[[ "${MAX_BATCHES}" =~ ^[0-9]+$ && "${MAX_BATCHES}" -ge 1 && "${MAX_BATCHES}" -le 100 ]] || fail "PATCHPONY_RETENTION_MAX_BATCHES must be 1..100"
command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1 || fail "Docker daemon is unavailable"
compose() { (cd "${COMPOSE_DIRECTORY}" && docker compose "$@"); }
psql() { compose exec -T postgres psql -v ON_ERROR_STOP=1 -U "${POSTGRES_USER:-patchpony}" -d "${POSTGRES_DB:-patchpony}" "$@"; }
preview() {
  psql -At -c "SELECT 'audit_events_180d=' || count(*) FROM patchpony.audit_events WHERE occurred_at < now() - interval '180 days' UNION ALL SELECT 'tool_invocations_90d=' || count(*) FROM patchpony.tool_invocations WHERE completed_at < now() - interval '90 days' UNION ALL SELECT 'idempotency_expired=' || count(*) FROM patchpony.idempotency_records WHERE expires_at < now() UNION ALL SELECT 'closed_sessions_90d=' || count(*) FROM patchpony.sessions WHERE status = 'Closed' AND status_changed_at < now() - interval '90 days' UNION ALL SELECT 'terminal_jobs_180d=' || count(*) FROM patchpony.jobs j WHERE status IN ('Closed','Failed','Cancelled') AND created_at < now() - interval '180 days' AND NOT EXISTS (SELECT 1 FROM patchpony.audit_events a WHERE a.job_id = j.id);"
}
delete_batch() {
  local name="$1" sql="$2" deleted=0 batch
  for ((batch=1; batch<=MAX_BATCHES; batch++)); do
    deleted="$(psql -At -c "${sql}")"
    [[ "${deleted}" =~ ^[0-9]+$ ]] || fail "unexpected delete result for ${name}"
    printf '%s\n' "${name}: deleted ${deleted} (batch ${batch}/${MAX_BATCHES})"
    (( deleted < BATCH_SIZE )) && break
  done
}
apply() {
  [[ "${CONFIRM}" == "DELETE_ONLY_APPROVED_PATCHPONY_RETENTION" ]] || fail "set PATCHPONY_RETENTION_CONFIRM=DELETE_ONLY_APPROVED_PATCHPONY_RETENTION after reviewing dry-run counts and a successful backup"
  delete_batch audit_events "WITH c AS (SELECT ctid FROM patchpony.audit_events WHERE occurred_at < now() - interval '180 days' ORDER BY occurred_at LIMIT ${BATCH_SIZE}), d AS (DELETE FROM patchpony.audit_events WHERE ctid IN (SELECT ctid FROM c) RETURNING 1) SELECT count(*) FROM d;"
  delete_batch tool_invocations "WITH c AS (SELECT ctid FROM patchpony.tool_invocations WHERE completed_at < now() - interval '90 days' ORDER BY completed_at LIMIT ${BATCH_SIZE}), d AS (DELETE FROM patchpony.tool_invocations WHERE ctid IN (SELECT ctid FROM c) RETURNING 1) SELECT count(*) FROM d;"
  delete_batch idempotency "WITH c AS (SELECT ctid FROM patchpony.idempotency_records WHERE expires_at < now() ORDER BY expires_at LIMIT ${BATCH_SIZE}), d AS (DELETE FROM patchpony.idempotency_records WHERE ctid IN (SELECT ctid FROM c) RETURNING 1) SELECT count(*) FROM d;"
  delete_batch closed_sessions "WITH c AS (SELECT ctid FROM patchpony.sessions WHERE status = 'Closed' AND status_changed_at < now() - interval '90 days' ORDER BY status_changed_at LIMIT ${BATCH_SIZE}), d AS (DELETE FROM patchpony.sessions WHERE ctid IN (SELECT ctid FROM c) RETURNING 1) SELECT count(*) FROM d;"
  delete_batch terminal_jobs "WITH c AS (SELECT j.ctid FROM patchpony.jobs j WHERE j.status IN ('Closed','Failed','Cancelled') AND j.created_at < now() - interval '180 days' AND NOT EXISTS (SELECT 1 FROM patchpony.audit_events a WHERE a.job_id = j.id) ORDER BY j.created_at LIMIT ${BATCH_SIZE}), d AS (DELETE FROM patchpony.jobs WHERE ctid IN (SELECT ctid FROM c) RETURNING 1) SELECT count(*) FROM d;"
  printf '%s\n' 'PatchPony retention apply: PASS'
}
case "${MODE}" in
  dry-run) preview ;;
  apply) apply ;;
  *) fail "usage: $0 [dry-run|apply]" ;;
esac