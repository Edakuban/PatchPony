# Service and bot credential rotation

I14.5 makes credential rotation an explicit, tested operating procedure. It covers the credentials presently configured for the PatchPony pilot and the external systems that must be changed in the same window:

| Boundary | Credential | Rotation owner | Verification |
| --- | --- | --- | --- |
| PostgreSQL | `POSTGRES_PASSWORD` | PatchPony operator | fresh password can connect; Gateway readiness passes |
| Gateway ← n8n | `PATCHPONY__AUTH__N8N__TOKEN` | PatchPony + n8n operator | n8n credential changed, then approved request succeeds |
| Gateway ↔ Worker | `PATCHPONY__WORKER__CLAIMSIGNINGKEY` | PatchPony operator | same new 32-byte Base64 key in both protected files; private worker active |
| Gateway → model provider | `PATCHPONY__AI__APIKEY` | PatchPony + model-provider operator | approved model-provider smoke test |
| Zoho → Gateway | `PATCHPONY__ZOHO__WEBHOOKSECRET` | PatchPony + Zoho operator | a newly signed controlled webhook validates |
| Gateway → Zoho | `PATCHPONY__ZOHO__ACCESSTOKEN` | PatchPony + Zoho operator | controlled task-comment integration test |
| Open WebUI → n8n | Pipe `N8N_WEBHOOK_TOKEN` | Open WebUI + n8n operator | Pipe request succeeds after both ends change |
| PatchPony → GitHub | repository-bound GitHub token, once publication is enabled | PatchPony + GitHub operator | least-privilege API check against the bound repository |

No secret value, digest, header or password belongs in ticket comments, shell history, logs, Git, screenshots or this repository. The current GitHub adapter already uses immutable per-request credential snapshots; it is not registered for a pilot project by default, but it remains part of every production rotation window once enabled.

## Protected files and candidate validation

On the Rootless application account, keep the active runtime and private-worker environment files outside the repository at mode `0600`. Generate complete replacement files through the approved secret manager; do not edit an active file in place. The candidate runtime file must include the six rotating PatchPony values plus `POSTGRES_DB` and `POSTGRES_USER`. The candidate worker file retains its image, session-root and worker identity entries but uses the same new signing key as the runtime candidate.

Validate candidates before touching an external provider or a running service:

```bash
export PATCHPONY_RUNTIME_ENV=/protected/path/patchpony-runtime.env
export PATCHPONY_RUNTIME_ENV_CANDIDATE=/protected/path/patchpony-runtime.next.env
export PATCHPONY_WORKER_ENV=$HOME/.config/patchpony/worker.env
export PATCHPONY_WORKER_ENV_CANDIDATE=/protected/path/worker.next.env
bash deploy/host/rotate-credentials.sh validate
```

[`rotate-credentials.sh`](../deploy/host/rotate-credentials.sh) checks file ownership boundaries (regular file, no symlink, mode `0600`), rejects blanks/placeholders and duplicates, requires every covered value to have changed, validates the 32-byte Base64 worker key and proves the Gateway/Worker candidate keys are identical. It never loads either file into the shell environment and never prints values.

## Controlled rotation window

1. Create new credentials in each provider and prepare both protected candidate files.
2. Change the external n8n, Zoho, Open WebUI and—when enabled—GitHub endpoints to their new approved credentials. Keep a documented, time-bounded rollback path at each provider.
3. Run `validate` again. Verify a console or break-glass access path before continuing.
4. Apply atomically from the application account:

```bash
export PATCHPONY_COMPOSE_DIRECTORY=/srv/patchpony
export PATCHPONY_CONFIRM_CREDENTIAL_ROTATION=I_HAVE_UPDATED_ALL_EXTERNAL_CREDENTIALS
export PATCHPONY_N8N_ROTATION_CONFIRMED=yes
export PATCHPONY_ZOHO_ROTATION_CONFIRMED=yes
export PATCHPONY_OPENWEBUI_ROTATION_CONFIRMED=yes
export PATCHPONY_GITHUB_ROTATION_CONFIRMED=yes
bash deploy/host/rotate-credentials.sh apply
bash deploy/host/rotate-credentials.sh audit
```

`apply` first verifies the new PostgreSQL password through TCP, then atomically replaces both 0600 files, recreates only the Gateway, restarts the separate Rootless Worker unit and checks Gateway readiness. If a local step fails, its exit trap attempts to restore the prior PostgreSQL password and both protected files, then restarts the old local services. The temporary rollback copies are mode `0600` and deleted on exit. A provider-side reversal still requires the documented external owner; do not treat local rollback as automatic cross-system rollback.

Finally run the controlled provider checks from the inventory, inspect redacted access/audit signals and revoke the previous credentials at every provider only after the new path is confirmed. Record only rotation time, systems, operator, result and external change references—never credential material.