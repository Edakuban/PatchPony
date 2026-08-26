# Threat model

## Scope and trust boundaries

PatchPony is an internal service between Open WebUI/n8n, approved project/vault checkouts, PostgreSQL, a Rootless sandbox worker, GitHub and Zoho. Untrusted inputs include tickets, repository and vault content, attachments, model output and all client parameters. The Gateway, project manifests, protected deployment configuration and the Rootless host account are trusted enforcement boundaries. There is no automatic merge capability.

## Threats and controls

| Threat | Primary controls | Evidence |
| --- | --- | --- |
| Prompt injection from tickets, code or vault | server-side tool/policy allowlists; untrusted content has no authority | `ProjectToolAuthorizationService`, `docs/open-webui-pipe.md` |
| Host/shell injection | fixed process arguments, command catalog, no free shell | `GitProcessRunner`, worker command catalog tests |
| Path traversal or symlink escape | canonical resolver, manifest policy, session path guard | `docs/session-rw-path-security.md` |
| Credential/host-file disclosure | no source mount for `.env`/`.git`; redaction; separate service credentials | `docker-compose.yml`, `GatewayLogRedactor` |
| Sandbox escape/resource exhaustion | Rootless daemon, non-root/read-only/cap-drop/no-network, seccomp/AppArmor, bounded CPU/RAM/PID/time | `docs/rootless-docker-production.md`, `docs/sandbox-security-profiles.md` |
| Network exfiltration | sandbox `network none`; internal data network; host Caddy edge only | `docs/production-network-boundary.md` |
| Web/API misuse | OIDC/service-token authentication, exact project/scope checks, rate/body/parallel limits | `docs/authentication.md`, `docs/gateway-limits.md` |
| Replay/duplicate publication | webhook idempotency, leases, explicit branch refs, no force/merge | `docs/zoho-webhook-idempotency.md`, publication tests |
| Malicious attachments | closed metadata schema, size/type limits, no archive execution | `ZohoWebhookValidator`, `TicketAttachmentTests` |
| Supply-chain compromise | dependency review, NuGet/Trivy scans, SBOMs, Dependabot | `docs/supply-chain-security.md` |
| Data retention/observability leakage | redacted audit contract, bounded retention, private monitoring | `docs/retention.md`, `docs/monitoring.md` |
| Data disclosure to model provider | configured external boundary, minimised pipe payload, no provider secrets in prompts | `docs/i6-privacy-review.md`, `docs/open-webui-pipe.md` |

## Assumptions and non-goals

The host, Docker Rootless daemon, OIDC provider, n8n/Open WebUI, GitHub, Zoho and model provider are operated according to their own hardened configurations. PatchPony does not protect against a compromised host administrator or a malicious approved reviewer. It also does not scan attachment contents for malware yet; attachments remain metadata-only in the current workflow.