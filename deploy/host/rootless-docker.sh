#!/usr/bin/env bash
# Rootless Docker preparation for the dedicated PatchPony sandbox account.
set -Eeuo pipefail

MODE="${1:-audit}"
DOCKER_USER="${PATCHPONY_DOCKER_USER:-patchpony-sandbox}"
SUBID_START="${PATCHPONY_SUBID_START:-}"
RANGE_SIZE=65536

fail() { printf '%s\n' "ERROR: $*" >&2; exit 1; }
require_debian_13() {
  [[ -r /etc/os-release ]] || fail "/etc/os-release is unavailable"
  # shellcheck disable=SC1091
  . /etc/os-release
  [[ "${ID:-}" == "debian" && "${VERSION_ID:-}" == "13" ]] || fail "this setup supports Debian 13 only"
}
require_root() { [[ "${EUID}" -eq 0 ]] || fail "run prepare as root"; }
require_sandbox_user() {
  [[ "${DOCKER_USER}" =~ ^[a-z_][a-z0-9_-]{0,31}$ ]] || fail "PATCHPONY_DOCKER_USER is invalid"
  id "${DOCKER_USER}" >/dev/null 2>&1 || fail "the dedicated sandbox user does not exist"
  [[ "${DOCKER_USER}" != "root" ]] || fail "root cannot own the sandbox daemon"
  id -nG "${DOCKER_USER}" | tr ' ' '\n' | grep -qx docker && fail "the sandbox user must not belong to the docker group"
}
require_cgroup_v2() { [[ "$(stat -fc %T /sys/fs/cgroup)" == "cgroup2fs" ]] || fail "rootless resource limits require cgroup v2"; }
rootful_daemon_inactive() {
  ! systemctl is-active --quiet docker.service && ! systemctl is-active --quiet docker.socket
}
range_is_sufficient() {
  local file="$1"
  awk -F: -v user="${DOCKER_USER}" -v minimum="${RANGE_SIZE}" '$1 == user && $3 >= minimum { found=1 } END { exit(found ? 0 : 1) }' "${file}"
}
range_is_free() {
  local file="$1" start="$2" end="$3"
  awk -F: -v start="${start}" -v end="${end}" '$2 ~ /^[0-9]+$/ && $3 ~ /^[0-9]+$/ { low=$2; high=$2+$3-1; if (start <= high && end >= low) conflict=1 } END { exit(conflict ? 1 : 0) }' "${file}"
}

prepare() {
  require_root
  require_debian_13
  require_sandbox_user
  require_cgroup_v2
  rootful_daemon_inactive || fail "disable and remove the rootful Docker daemon before enabling the rootless sandbox daemon"
  [[ "${SUBID_START}" =~ ^[0-9]+$ ]] || fail "set PATCHPONY_SUBID_START to an approved free subordinate-ID range start"
  local end=$((SUBID_START + RANGE_SIZE - 1))
  (( SUBID_START >= 100000 && end > SUBID_START )) || fail "the subordinate-ID range is unsafe"
  export DEBIAN_FRONTEND=noninteractive
  apt-get update
  apt-get install --yes --no-install-recommends uidmap dbus-user-session slirp4netns fuse-overlayfs
  for mapping in /etc/subuid /etc/subgid; do
    if ! range_is_sufficient "${mapping}"; then
      range_is_free "${mapping}" "${SUBID_START}" "${end}" || fail "the requested subordinate-ID range overlaps ${mapping}"
    fi
  done
  for mapping in /etc/subuid /etc/subgid; do
    if ! range_is_sufficient "${mapping}"; then
      printf '%s:%s:%s\n' "${DOCKER_USER}" "${SUBID_START}" "${RANGE_SIZE}" >>"${mapping}"
    fi
  done
  local uid
  uid="$(id -u "${DOCKER_USER}")"
  install -d -m 0755 "/etc/systemd/system/user@${uid}.service.d"
  cat >"/etc/systemd/system/user@${uid}.service.d/patchpony-rootless-docker.conf" <<'EOF'
[Service]
Delegate=cpu cpuset io memory pids
EOF
  systemctl daemon-reload
  loginctl enable-linger "${DOCKER_USER}"
  printf '%s\n' "Preparation complete. Log in as ${DOCKER_USER} through a real PAM/systemd session and run: bash deploy/host/rootless-docker.sh activate"
}

activate() {
  require_debian_13
  require_sandbox_user
  [[ "${EUID}" -ne 0 ]] || fail "run activate as the dedicated sandbox user, never as root"
  [[ "$(id -un)" == "${DOCKER_USER}" ]] || fail "activate must run as PATCHPONY_DOCKER_USER"
  command -v dockerd-rootless-setuptool.sh >/dev/null 2>&1 || fail "install Docker Engine plus docker-ce-rootless-extras from the approved package source first"
  [[ -n "${XDG_RUNTIME_DIR:-}" && -S "${XDG_RUNTIME_DIR}/bus" ]] || fail "log in through PAM/systemd; do not use sudo su for rootless Docker activation"
  dockerd-rootless-setuptool.sh install
  systemctl --user enable --now docker
  audit
}

audit() {
  require_debian_13
  require_sandbox_user
  [[ "${EUID}" -ne 0 ]] || fail "run audit as the dedicated sandbox user in its systemd user session"
  [[ "$(id -un)" == "${DOCKER_USER}" ]] || fail "audit must run as PATCHPONY_DOCKER_USER"
  require_cgroup_v2
  command -v newuidmap >/dev/null 2>&1 && command -v newgidmap >/dev/null 2>&1 || fail "uidmap tools are missing"
  range_is_sufficient /etc/subuid || fail "subordinate UID range is missing or too small"
  range_is_sufficient /etc/subgid || fail "subordinate GID range is missing or too small"
  systemctl --user is-enabled --quiet docker && systemctl --user is-active --quiet docker || fail "rootless docker.service is not enabled and active"
  [[ -S "/run/user/$(id -u)/docker.sock" ]] || fail "the expected rootless Docker socket is missing"
  docker info --format '{{json .SecurityOptions}}' | grep -Fq 'name=rootless' || fail "Docker is not connected to a rootless daemon"
  [[ "$(docker info --format '{{.CgroupDriver}}')" == "systemd" ]] || fail "rootless resource limits require the systemd cgroup driver"
  docker context show | grep -qx rootless || fail "the Docker CLI is not using the rootless context"
  printf '%s\n' 'PatchPony rootless Docker: PASS'
}

case "${MODE}" in
  prepare) prepare ;;
  activate) activate ;;
  audit) audit ;;
  *) fail "usage: $0 [prepare|activate|audit]" ;;
esac