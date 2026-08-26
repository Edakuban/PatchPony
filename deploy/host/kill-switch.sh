#!/usr/bin/env bash
# Reversible PatchPony containment switch. It stops application execution, never deletes data.
set -Eeuo pipefail
MODE="${1:-status}"
COMPOSE_DIRECTORY="${PATCHPONY_KILLSWITCH_COMPOSE_DIRECTORY:-}"
RUNTIME_ENV="${PATCHPONY_KILLSWITCH_RUNTIME_ENV:-}"
CONFIRM="${PATCHPONY_KILLSWITCH_CONFIRM:-}"
fail(){ printf '%s\n' "ERROR: $*" >&2; exit 1; }
[[ -n "${COMPOSE_DIRECTORY}" && -d "${COMPOSE_DIRECTORY}" ]] || fail "set PATCHPONY_KILLSWITCH_COMPOSE_DIRECTORY"
[[ -n "${RUNTIME_ENV}" && -f "${RUNTIME_ENV}" && ! -L "${RUNTIME_ENV}" ]] || fail "set PATCHPONY_KILLSWITCH_RUNTIME_ENV to a protected regular file"
[[ "$(stat -c '%a' "${RUNTIME_ENV}")" == "600" ]] || fail "runtime environment file must be mode 0600"
command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1 || fail "Docker daemon is unavailable"
compose(){ (cd "${COMPOSE_DIRECTORY}" && docker compose --env-file "${RUNTIME_ENV}" -f docker-compose.yml -f deploy/production/docker-compose.rootless.yml "$@"); }
status(){ compose ps gateway; systemctl --user is-active patchpony-sandbox-worker.service || true; }
disable(){ [[ "${CONFIRM}" == "DISABLE_PATCHPONY_EXECUTION" ]] || fail "set PATCHPONY_KILLSWITCH_CONFIRM=DISABLE_PATCHPONY_EXECUTION"; compose stop gateway; systemctl --user stop patchpony-sandbox-worker.service; printf '%s\n' 'PatchPony kill switch: gateway and worker stopped; database and volumes unchanged.'; }
enable(){ [[ "${CONFIRM}" == "ENABLE_PATCHPONY_AFTER_INCIDENT_REVIEW" ]] || fail "set PATCHPONY_KILLSWITCH_CONFIRM=ENABLE_PATCHPONY_AFTER_INCIDENT_REVIEW"; compose up -d gateway; systemctl --user start patchpony-sandbox-worker.service; compose exec -T gateway wget --spider -q http://localhost:8080/health/ready; systemctl --user is-active --quiet patchpony-sandbox-worker.service; printf '%s\n' 'PatchPony kill switch: gateway and worker restored.'; }
case "${MODE}" in status) status;; disable) disable;; enable) enable;; *) fail "usage: $0 [status|disable|enable]";; esac