# Debian 13 production-host baseline

[`deploy/host/debian-13-baseline.sh`](../deploy/host/debian-13-baseline.sh) is the reproducible I14.1 baseline for a dedicated PatchPony Debian 13 host. It installs and enables automatic security updates, AppArmor, `auditd` and time synchronization; then it applies a narrowly scoped SSH hardening drop-in.

## Safety boundary

The script does **not** configure Docker, container privileges, firewall rules, Caddy, DNS, PostgreSQL, application secrets or a PatchPony deployment. Those decisions belong to I14.2 and I14.3 onward. It never downloads or executes remote scripts.

`apply` changes a host and must be run by its operator from a local console or after a separate, verified non-root key-based SSH session is open. Before it changes SSH, it requires:

```bash
export PATCHPONY_SSH_ADMIN_USER=<existing-non-root-sudo-user>
export PATCHPONY_CONFIRM_SSH_HARDENING=I_HAVE_A_SECOND_KEY_SESSION
sudo -E bash deploy/host/debian-13-baseline.sh apply
```

The supplied user must exist, belong to `sudo`, and have a non-empty `~/.ssh/authorized_keys`. The script validates `sshd` before reload; on validation failure it removes only its own drop-in. Afterwards, retain the second session and verify a fresh key-only login before closing the original one.

## Verification and evidence

Run the non-mutating verification after each host change and record only its pass/fail result plus host inventory reference in the deployment record:

```bash
sudo bash deploy/host/debian-13-baseline.sh audit
```

A passing baseline means package presence, enabled and active `apparmor`, `auditd`, `chrony`, enabled APT timers, the automatic-update policy, the SSH drop-in, effective SSH key-only settings and NTP synchronization were all checked. It does not claim that a firewall or Docker boundary exists.

Debian documents `unattended-upgrades` as the backend for periodic automatic package upgrades and its APT configuration files; AppArmor provides enforced or complain-mode program confinement and integrates with the audit system. See the [Debian unattended-upgrades manual](https://manpages.debian.org/trixie/unattended-upgrades/unattended-upgrades.8.en.html) and [Debian AppArmor manual](https://manpages.debian.org/trixie/apparmor/apparmor.7.en.html).