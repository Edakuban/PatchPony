#!/usr/bin/env bash
# Isolated PostgreSQL backup/restore rehearsal. It never connects to PatchPony's configured database.
set -Eeuo pipefail

IMAGE="${PATCHPONY_POSTGRES_IMAGE:-postgres:18-alpine}"
RUN_ID="$(date -u +%Y%m%d%H%M%S)-$$-$RANDOM"
PREFIX="patchpony-restore-test-${RUN_ID}"
NETWORK="${PREFIX}-network"
SOURCE_VOLUME="${PREFIX}-source"
TARGET_VOLUME="${PREFIX}-target"
BACKUP_VOLUME="${PREFIX}-backup"
SOURCE_CONTAINER="${PREFIX}-source"
TARGET_CONTAINER="${PREFIX}-target"
DATABASE="patchpony_restore_test"
DATABASE_USER="patchpony_restore_test"
DATABASE_PASSWORD="rehearsal-${RUN_ID}-password"
SENTINEL="restore-${RUN_ID}"

fail() { printf '%s\n' "ERROR: $*" >&2; exit 1; }
cleanup() {
  docker rm --force "${SOURCE_CONTAINER}" "${TARGET_CONTAINER}" >/dev/null 2>&1 || true
  docker network rm "${NETWORK}" >/dev/null 2>&1 || true
  docker volume rm "${SOURCE_VOLUME}" "${TARGET_VOLUME}" "${BACKUP_VOLUME}" >/dev/null 2>&1 || true
}
require_docker() {
  command -v docker >/dev/null 2>&1 || fail "Docker CLI is required"
  docker info >/dev/null 2>&1 || fail "Docker daemon is unavailable (set DOCKER_HOST for the rootless daemon if applicable)"
}
wait_for_database() {
  local container="$1"
  local attempt
  for attempt in {1..60}; do
    if docker exec -e "PGPASSWORD=${DATABASE_PASSWORD}" "${container}" pg_isready -U "${DATABASE_USER}" -d "${DATABASE}" >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  fail "PostgreSQL container ${container} did not become ready within 60 seconds"
}

trap cleanup EXIT
require_docker

printf '%s\n' "Starting isolated PostgreSQL restore rehearsal ${RUN_ID}."
docker network create --label "patchpony.restore-rehearsal=${RUN_ID}" "${NETWORK}" >/dev/null
docker volume create --label "patchpony.restore-rehearsal=${RUN_ID}" "${SOURCE_VOLUME}" >/dev/null
docker volume create --label "patchpony.restore-rehearsal=${RUN_ID}" "${TARGET_VOLUME}" >/dev/null
docker volume create --label "patchpony.restore-rehearsal=${RUN_ID}" "${BACKUP_VOLUME}" >/dev/null

docker run --detach --name "${SOURCE_CONTAINER}" --network "${NETWORK}" \
  --label "patchpony.restore-rehearsal=${RUN_ID}" \
  --mount "type=volume,source=${SOURCE_VOLUME},target=/var/lib/postgresql" \
  --mount "type=volume,source=${BACKUP_VOLUME},target=/backup" \
  -e "POSTGRES_DB=${DATABASE}" -e "POSTGRES_USER=${DATABASE_USER}" -e "POSTGRES_PASSWORD=${DATABASE_PASSWORD}" \
  "${IMAGE}" >/dev/null
wait_for_database "${SOURCE_CONTAINER}"

docker exec -e "PGPASSWORD=${DATABASE_PASSWORD}" "${SOURCE_CONTAINER}" psql -v ON_ERROR_STOP=1 -U "${DATABASE_USER}" -d "${DATABASE}" \
  -c "CREATE TABLE restore_probe (id integer PRIMARY KEY, marker text NOT NULL, payload integer NOT NULL); INSERT INTO restore_probe (id, marker, payload) VALUES (1, '${SENTINEL}', 42);" >/dev/null

docker exec -e "PGPASSWORD=${DATABASE_PASSWORD}" "${SOURCE_CONTAINER}" pg_dump --format=custom --file=/backup/patchpony.dump -U "${DATABASE_USER}" "${DATABASE}"
ARCHIVE_SHA256="$(docker exec "${SOURCE_CONTAINER}" sha256sum /backup/patchpony.dump | awk '{print $1}')"
[[ "${ARCHIVE_SHA256}" =~ ^[a-f0-9]{64}$ ]] || fail "backup archive checksum could not be read"
docker exec "${SOURCE_CONTAINER}" pg_restore --list /backup/patchpony.dump | grep -Fq "restore_probe" || fail "backup archive does not contain the probe table"

docker run --detach --name "${TARGET_CONTAINER}" --network "${NETWORK}" \
  --label "patchpony.restore-rehearsal=${RUN_ID}" \
  --mount "type=volume,source=${TARGET_VOLUME},target=/var/lib/postgresql" \
  --mount "type=volume,source=${BACKUP_VOLUME},target=/backup,readonly" \
  -e "POSTGRES_DB=${DATABASE}" -e "POSTGRES_USER=${DATABASE_USER}" -e "POSTGRES_PASSWORD=${DATABASE_PASSWORD}" \
  "${IMAGE}" >/dev/null
wait_for_database "${TARGET_CONTAINER}"

docker exec -e "PGPASSWORD=${DATABASE_PASSWORD}" "${TARGET_CONTAINER}" pg_restore --clean --if-exists --no-owner --no-privileges --exit-on-error -U "${DATABASE_USER}" -d "${DATABASE}" /backup/patchpony.dump
RESTORED="$(docker exec -e "PGPASSWORD=${DATABASE_PASSWORD}" "${TARGET_CONTAINER}" psql -v ON_ERROR_STOP=1 -Atq -U "${DATABASE_USER}" -d "${DATABASE}" -c "SELECT marker || ':' || payload FROM restore_probe WHERE id = 1")"
[[ "${RESTORED}" == "${SENTINEL}:42" ]] || fail "restored sentinel differs from the archived source"

printf '%s\n' "Backup archive SHA-256: ${ARCHIVE_SHA256}"
printf '%s\n' "PatchPony PostgreSQL backup/restore rehearsal: PASS"