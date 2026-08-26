#!/usr/bin/env bash
# PatchPony Debian 13 host baseline. Run only from a local console or a verified second SSH session.
set -Eeuo pipefail

MODE="${1:-audit}"
ADMIN_USER="${PATCHPONY_SSH_ADMIN_USER:-}"
CONFIRM="${PATCHPONY_CONFIRM_SSH_HARDENING:-}"
SSH_DROPIN="/etc/ssh/sshd_config.d/60-patchpony-baseline.conf"
APT_PERIODIC="/etc/apt/apt.conf.d/20auto-upgrades"

fail() { printf '%s\n' "ERROR: $*" >&2; exit 1; }
require_root() { [[ "${EUID}" -eq 0 ]] || fail "run as root"; }
require_debian_13() {
  [[ -r /etc/os-release ]] || fail "/etc/os-release is unavailable"
  # shellcheck disable=SC1091
  . /etc/os-release
  [[ "${ID:-}" == "debian" && "${VERSION_ID:-}" == "13" ]] || fail "this baseline supports Debian 13 only"
}
require_admin_key() {
  [[ -n "${ADMIN_USER}" ]] || fail "set PATCHPONY_SSH_ADMIN_USER to the verified non-root key administrator"
  [[ "${ADMIN_USER}" != "root" ]] || fail "the SSH administrator must not be root"
  id "${ADMIN_USER}" >/dev/null 2>&1 || fail "the SSH administrator does not exist"
  id -nG "${ADMIN_USER}" | tr ' ' '\n' | grep -qx sudo || fail "the SSH administrator is not in sudo"
  local home
  home="$(getent passwd "${ADMIN_USER}" | cut -d: -f6)"
  [[ -n "${home}" && -s "${home}/.ssh/authorized_keys" ]] || fail "the SSH administrator has no authorized_keys file"
}
require_confirmation() {
  [[ "${CONFIRM}" == "I_HAVE_A_SECOND_KEY_SESSION" ]] || fail "set PATCHPONY_CONFIRM_SSH_HARDENING=I_HAVE_A_SECOND_KEY_SESSION only after verifying another key-based admin session or local console access"
}
check_service() { systemctl is-enabled --quiet "$1" && systemctl is-active --quiet "$1"; }

verify() {
  require_debian_13
  dpkg-query -W -f='${db:Status-Status}\n' unattended-upgrades auditd apparmor apparmor-utils chrony 2>/dev/null | grep -qx installed || fail "required baseline packages are missing"
  check_service apparmor || fail "AppArmor is not active and enabled"
  check_service auditd || fail "auditd is not active and enabled"
  check_service chrony || fail "chrony is not active and enabled"
  systemctl is-enabled --quiet apt-daily.timer && systemctl is-enabled --quiet apt-daily-upgrade.timer || fail "APT update timers are not enabled"
  [[ -r "${APT_PERIODIC}" ]] || fail "automatic update policy is missing"
  grep -Fqx 'APT::Periodic::Unattended-Upgrade "1";' "${APT_PERIODIC}" || fail "unattended upgrades are not enabled"
  [[ -r "${SSH_DROPIN}" ]] || fail "PatchPony SSH drop-in is missing"
  sshd -t || fail "effective SSH configuration is invalid"
  sshd -T | grep -qx 'permitrootlogin no' || fail "root SSH login is not disabled"
  sshd -T | grep -qx 'passwordauthentication no' || fail "SSH password authentication is not disabled"
  sshd -T | grep -qx 'kbdinteractiveauthentication no' || fail "SSH keyboard-interactive authentication is not disabled"
  sshd -T | grep -qx 'pubkeyauthentication yes' || fail "SSH public-key authentication is not enabled"
  aa-status --enabled >/dev/null 2>&1 || fail "AppArmor kernel support is unavailable"
  timedatectl show -p NTPSynchronized --value | grep -qi '^yes$' || fail "time synchronization is not active"
  printf '%s\n' 'PatchPony Debian 13 baseline: PASS'
}

apply() {
  require_root
  require_debian_13
  require_admin_key
  require_confirmation
  export DEBIAN_FRONTEND=noninteractive
  apt-get update
  apt-get install --yes --no-install-recommends unattended-upgrades auditd apparmor apparmor-utils chrony
  install -d -m 0755 /etc/apt/apt.conf.d /etc/ssh/sshd_config.d
  cat >"${APT_PERIODIC}" <<'EOF'
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Unattended-Upgrade "1";
EOF
  cat >"${SSH_DROPIN}" <<'EOF'
PermitRootLogin no
PasswordAuthentication no
KbdInteractiveAuthentication no
PubkeyAuthentication yes
X11Forwarding no
MaxAuthTries 3
LoginGraceTime 30
ClientAliveInterval 300
ClientAliveCountMax 2
EOF
  if ! sshd -t; then
    rm -f "${SSH_DROPIN}"
    fail "SSH validation failed; the PatchPony drop-in was removed before reload"
  fi
  systemctl enable --now apparmor auditd chrony
  systemctl enable apt-daily.timer apt-daily-upgrade.timer
  systemctl reload ssh
  verify
}

case "${MODE}" in
  audit) verify ;;
  apply) apply ;;
  *) fail "usage: $0 [audit|apply]" ;;
esac