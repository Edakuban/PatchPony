#!/usr/bin/env bash
# Controlled service-credential rotation for the Rootless PatchPony deployment.
# The operator supplies complete protected replacement env files; values are never logged.
set -Eeuo pipefail
umask 077

MODE="${1:-validate}"
RUNTIME_ENV="${PATCHPONY_RUNTIME_ENV:-}"
RUNTIME_CANDIDATE="${PATCHPONY_RUNTIME_ENV_CANDIDATE:-}"
WORKER_ENV="${PATCHPONY_WORKER_ENV:-}"
WORKER_CANDIDATE="${PATCHPONY_WORKER_ENV_CANDIDATE:-}"
COMPOSE_DIRECTORY="${PATCHPONY_COMPOSE_DIRECTORY:-}"
CONFIRM="${PATCHPONY_CONFIRM_CREDENTIAL_ROTATION:-}"
N8N_CONFIRMED="${PATCHPONY_N8N_ROTATION_CONFIRMED:-}"
ZOHO_CONFIRMED="${PATCHPONY_ZOHO_ROTATION_CONFIRMED:-}"
OWUI_CONFIRMED="${PATCHPONY_OPENWEBUI_ROTATION_CONFIRMED:-}"
GITHUB_CONFIRMED="${PATCHPONY_GITHUB_ROTATION_CONFIRMED:-}"
RUN_ID="$(date -u +%Y%m%d%H%M%S)-$$"
RUNTIME_BACKUP=""
WORKER_BACKUP=""
POSTGRES_CHANGED=0
FILES_CHANGED=0

fail() { printf '%s\n' "ERROR: $*" >&2; exit 1; }
require_file_0600() {
  local file="$1"
  [[ -f "${file}" && ! -L "${file}" ]] || fail "protected env file is missing or is a symlink"
  [[ "$(stat -c '%a' "${file}")" == "600" ]] || fail "protected env file must have mode 0600"
}
value_for() {
  local file="$1" key="$2" matches value
  matches="$(grep -Ec "^${key}=" "${file}" || true)"
  [[ "${matches}" == "1" ]] || fail "${key} must occur exactly once in ${file}"
  value="$(sed -n "s/^${key}=//p" "${file}")"
  [[ -n "${value}" && "${value}" != *$'\r'* && "${value}" != "change-me-for-local-development" ]] || fail "${key} is empty or a placeholder"
  printf '%s' "${value}"
}
validate_key() {
  local file="$1" key="$2"
  value_for "${file}" "${key}" >/dev/null
}
validate_hmac_key() {
  local value decoded_length
  value="$(value_for "$1" PATCHPONY__WORKER__CLAIMSIGNINGKEY)"
  decoded_length="$(printf '%s' "${value}" | base64 --decode 2>/dev/null | wc -c)" || fail "worker signing key is not Base64"
  [[ "${decoded_length}" == "32" ]] || fail "worker signing key must decode to exactly 32 bytes"
}
validate_env() {
  local file="$1"
  require_file_0600 "${file}"
  validate_key "${file}" POSTGRES_DB
  validate_key "${file}" POSTGRES_USER
  validate_key "${file}" POSTGRES_PASSWORD
  validate_key "${file}" PATCHPONY__AUTH__N8N__TOKEN
  validate_key "${file}" PATCHPONY__AI__APIKEY
  validate_key "${file}" PATCHPONY__ZOHO__WEBHOOKSECRET
  validate_key "${file}" PATCHPONY__ZOHO__ACCESSTOKEN
  validate_hmac_key "${file}"
}
validate_worker_env() {
  require_file_0600 "$1"
  validate_key "$1" PATCHPONY__WORKER__ID
  validate_hmac_key "$1"
}
require_unchanged() {
  local key="$1" old new
  old="$(value_for "${RUNTIME_ENV}" "${key}")"
  new="$(value_for "${RUNTIME_CANDIDATE}" "${key}")"
  [[ "${old}" == "${new}" ]] || fail "credential rotation must not change ${key}"
}
require_changed() {
  local key="$1" old new
  old="$(value_for "${RUNTIME_ENV}" "${key}")"
  new="$(value_for "${RUNTIME_CANDIDATE}" "${key}")"
  [[ "${old}" != "${new}" ]] || fail "candidate did not rotate ${key}"
}
validate_rotation() {
  [[ -n "${RUNTIME_ENV}" && -n "${RUNTIME_CANDIDATE}" && -n "${WORKER_ENV}" && -n "${WORKER_CANDIDATE}" ]] || fail "set all four PATCHPONY_*_ENV paths"
  validate_env "${RUNTIME_ENV}"
  validate_env "${RUNTIME_CANDIDATE}"
  validate_worker_env "${WORKER_ENV}"
  validate_worker_env "${WORKER_CANDIDATE}"
  local key
  for key in POSTGRES_PASSWORD PATCHPONY__AUTH__N8N__TOKEN PATCHPONY__WORKER__CLAIMSIGNINGKEY PATCHPONY__AI__APIKEY PATCHPONY__ZOHO__WEBHOOKSECRET PATCHPONY__ZOHO__ACCESSTOKEN; do require_changed "${key}"; done
  [[ "$(value_for "${RUNTIME_CANDIDATE}" PATCHPONY__WORKER__CLAIMSIGNINGKEY)" == "$(value_for "${WORKER_CANDIDATE}" PATCHPONY__WORKER__CLAIMSIGNINGKEY)" ]] || fail "Gateway and worker candidate signing keys differ"
  printf '%s\n' 'Credential candidate validation: PASS (values intentionally not displayed)'
}
compose() {
  docker compose --env-file "${RUNTIME_ENV}" -f docker-compose.yml -f deploy/production/docker-compose.rootless.yml "$@"
}
sql_escape_literal() { printf '%s' "$1" | sed "s/'/''/g"; }
rollback() {
  local exit_code="$?"
  if [[ "${exit_code}" -ne 0 ]]; then
    printf '%s\n' 'Credential rotation failed; attempting local rollback without printing secret values.' >&2
    if [[ "${POSTGRES_CHANGED}" == "1" && -n "${COMPOSE_DIRECTORY}" && -n "${RUNTIME_BACKUP}" ]]; then
      local db_user old_password escaped
      db_user="$(value_for "${RUNTIME_BACKUP}" POSTGRES_USER)"
      old_password="$(value_for "${RUNTIME_BACKUP}" POSTGRES_PASSWORD)"
      escaped="$(sql_escape_literal "${old_password}")"
      (cd "${COMPOSE_DIRECTORY}" && compose exec -T postgres psql -v ON_ERROR_STOP=1 -U "${db_user}" -d postgres -c "ALTER ROLE \"${db_user}\" PASSWORD '${escaped}'") >/dev/null 2>&1 || true
    fi
    if [[ "${FILES_CHANGED}" == "1" && -n "${RUNTIME_BACKUP}" && -n "${WORKER_BACKUP}" ]]; then
      install -m 0600 "${RUNTIME_BACKUP}" "${RUNTIME_ENV}" || true
      install -m 0600 "${WORKER_BACKUP}" "${WORKER_ENV}" || true
      [[ -n "${COMPOSE_DIRECTORY}" ]] && (cd "${COMPOSE_DIRECTORY}" && compose up -d --force-recreate gateway >/dev/null 2>&1) || true
      systemctl --user restart patchpony-sandbox-worker.service >/dev/null 2>&1 || true
    fi
  fi
  [[ -n "${RUNTIME_BACKUP}" ]] && rm -f -- "${RUNTIME_BACKUP}"
  [[ -n "${WORKER_BACKUP}" ]] && rm -f -- "${WORKER_BACKUP}"
}
apply_rotation() {
  validate_rotation
  [[ -n "${COMPOSE_DIRECTORY}" && -d "${COMPOSE_DIRECTORY}" ]] || fail "set PATCHPONY_COMPOSE_DIRECTORY to the protected deployment checkout"
  [[ "${CONFIRM}" == "I_HAVE_UPDATED_ALL_EXTERNAL_CREDENTIALS" ]] || fail "set PATCHPONY_CONFIRM_CREDENTIAL_ROTATION only after the external side is prepared"
  [[ "${N8N_CONFIRMED}" == "yes" && "${ZOHO_CONFIRMED}" == "yes" && "${OWUI_CONFIRMED}" == "yes" && "${GITHUB_CONFIRMED}" == "yes" ]] || fail "confirm n8n, Zoho, Open WebUI and GitHub credential updates before applying"
  command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1 || fail "Docker daemon is unavailable"
  command -v systemctl >/dev/null 2>&1 || fail "systemctl is required for the private worker"

  RUNTIME_BACKUP="$(mktemp)"
  WORKER_BACKUP="$(mktemp)"
  install -m 0600 "${RUNTIME_ENV}" "${RUNTIME_BACKUP}"
  install -m 0600 "${WORKER_ENV}" "${WORKER_BACKUP}"
  local db_user new_password escaped
  db_user="$(value_for "${RUNTIME_CANDIDATE}" POSTGRES_USER)"
  [[ "${db_user}" =~ ^[a-z_][a-z0-9_]{0,62}$ ]] || fail "POSTGRES_USER must be a safe PostgreSQL role name"
  new_password="$(value_for "${RUNTIME_CANDIDATE}" POSTGRES_PASSWORD)"
  escaped="$(sql_escape_literal "${new_password}")"

  (
    cd "${COMPOSE_DIRECTORY}"
    compose exec -T postgres psql -v ON_ERROR_STOP=1 -U "${db_user}" -d postgres -c "ALTER ROLE \"${db_user}\" PASSWORD '${escaped}'" >/dev/null
    POSTGRES_CHANGED=1
    compose exec -T -e "PGPASSWORD=${new_password}" postgres psql -v ON_ERROR_STOP=1 -h 127.0.0.1 -U "${db_user}" -d "$(value_for "${RUNTIME_CANDIDATE}" POSTGRES_DB)" -c 'SELECT 1' >/dev/null
  )
  POSTGRES_CHANGED=1

  install -m 0600 "${RUNTIME_CANDIDATE}" "${RUNTIME_ENV}"
  install -m 0600 "${WORKER_CANDIDATE}" "${WORKER_ENV}"
  FILES_CHANGED=1
  (
    cd "${COMPOSE_DIRECTORY}"
    compose up -d --force-recreate gateway >/dev/null
    compose exec -T gateway wget --spider -q http://localhost:8080/health/ready
  )
  systemctl --user restart patchpony-sandbox-worker.service
  systemctl --user is-active --quiet patchpony-sandbox-worker.service || fail "private worker is not active after rotation"
  printf '%s\n' "PatchPony credential rotation ${RUN_ID}: PASS"
}
audit_rotation() {
  [[ -n "${RUNTIME_ENV}" && -n "${WORKER_ENV}" ]] || fail "set PATCHPONY_RUNTIME_ENV and PATCHPONY_WORKER_ENV"
  validate_env "${RUNTIME_ENV}"
  validate_worker_env "${WORKER_ENV}"
  [[ "$(value_for "${RUNTIME_ENV}" PATCHPONY__WORKER__CLAIMSIGNINGKEY)" == "$(value_for "${WORKER_ENV}" PATCHPONY__WORKER__CLAIMSIGNINGKEY)" ]] || fail "Gateway and worker signing keys differ"
  [[ -n "${COMPOSE_DIRECTORY}" && -d "${COMPOSE_DIRECTORY}" ]] || fail "set PATCHPONY_COMPOSE_DIRECTORY for live service checks"
  (
    cd "${COMPOSE_DIRECTORY}"
    compose ps --status running postgres gateway | grep -q postgres || fail "PostgreSQL is not running"
    compose exec -T gateway wget --spider -q http://localhost:8080/health/ready
  )
  systemctl --user is-active --quiet patchpony-sandbox-worker.service || fail "private worker is not active"
  printf '%s\n' 'Credential rotation audit: PASS (values intentionally not displayed)'
}

trap rollback EXIT
case "${MODE}" in
  validate) validate_rotation ;;
  apply) apply_rotation ;;
  audit) audit_rotation ;;
  *) fail "usage: $0 [validate|apply|audit]" ;;
esac