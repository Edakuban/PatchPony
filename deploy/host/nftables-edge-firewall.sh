#!/usr/bin/env bash
# Dedicated-host nftables edge policy for PatchPony. Run from a local console or verified second SSH session.
set -Eeuo pipefail

MODE="${1:-audit}"
SSH_PORT="${PATCHPONY_SSH_PORT:-}"
CONFIRM="${PATCHPONY_CONFIRM_FIREWALL:-}"
RULESET="/etc/nftables.conf"
CANDIDATE="/run/patchpony-nftables.conf"

fail() { printf '%s\n' "ERROR: $*" >&2; exit 1; }
require_root() { [[ "${EUID}" -eq 0 ]] || fail "run as root"; }
require_debian_13() {
  [[ -r /etc/os-release ]] || fail "/etc/os-release is unavailable"
  # shellcheck disable=SC1091
  . /etc/os-release
  [[ "${ID:-}" == "debian" && "${VERSION_ID:-}" == "13" ]] || fail "this setup supports Debian 13 only"
}
require_ssh_port() {
  [[ "${SSH_PORT}" =~ ^[0-9]+$ && "${SSH_PORT}" -ge 1 && "${SSH_PORT}" -le 65535 ]] || fail "set PATCHPONY_SSH_PORT to the active SSH port"
  sshd -T | grep -qx "port ${SSH_PORT}" || fail "PATCHPONY_SSH_PORT does not match effective sshd configuration"
}
render() {
  cat >"${CANDIDATE}" <<EOF
table inet patchpony {
  chain input {
    type filter hook input priority filter; policy drop;
    ct state established,related accept
    iifname "lo" accept
    ip protocol icmp accept
    ip6 nexthdr ipv6-icmp accept
    tcp dport ${SSH_PORT} ct state new accept
    tcp dport { 80, 443 } ct state new accept
    counter reject with icmpx type admin-prohibited
  }
  chain forward {
    type filter hook forward priority filter; policy drop;
  }
  chain output {
    type filter hook output priority filter; policy accept;
  }
}
EOF
}

apply() {
  require_root
  require_debian_13
  require_ssh_port
  [[ "${CONFIRM}" == "I_HAVE_A_SECOND_KEY_SESSION" ]] || fail "set PATCHPONY_CONFIRM_FIREWALL=I_HAVE_A_SECOND_KEY_SESSION only after verifying a second key-based SSH session or local console access"
  export DEBIAN_FRONTEND=noninteractive
  apt-get update
  apt-get install --yes --no-install-recommends nftables
  render
  nft -c -f "${CANDIDATE}" || fail "candidate nftables policy is invalid"
  [[ -f "${RULESET}" ]] && install -m 0600 "${RULESET}" "${RULESET}.patchpony-backup"
  install -m 0600 "${CANDIDATE}" "${RULESET}"
  nft -f "${RULESET}"
  systemctl enable --now nftables
  audit
}

audit() {
  require_root
  require_debian_13
  require_ssh_port
  systemctl is-enabled --quiet nftables && systemctl is-active --quiet nftables || fail "nftables is not enabled and active"
  nft list chain inet patchpony input | grep -Fq "tcp dport ${SSH_PORT}" || fail "SSH port is not allowed"
  nft list chain inet patchpony input | grep -Fq "80, 443" || fail "only HTTP(S) edge ports are not present"
  nft list chain inet patchpony input | grep -Fq 'policy drop' || fail "input policy is not default drop"
  nft list chain inet patchpony forward | grep -Fq 'policy drop' || fail "forward policy is not default drop"
  printf '%s\n' 'PatchPony edge firewall: PASS'
}

case "${MODE}" in
  apply) apply ;;
  audit) audit ;;
  *) fail "usage: $0 [apply|audit]" ;;
esac