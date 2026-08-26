# I14.10 security review

**Review date:** 2026-08-26\
**Scope:** current repository implementation and deployment artifacts; no production host, external provider or real pilot data was accessed.

## Result

The design review finds the implemented controls consistent with the threat model for a restricted internal pilot. It is **not a production-launch approval**. The following evidence must exist before I14.12: a real isolated load-test result, host baseline/rootless/firewall audits, a backup/restore exercise on the intended host, and configured alert delivery.

## Review matrix

| Area | Review result | Evidence | Residual risk / required action |
| --- | --- | --- | --- |
| Prompt and content injection | pass | policy, manifest and bounded-tool integration tests | Human reviewers must treat all model output as untrusted. |
| Authentication and authorization | pass in code | policy/security tests; OIDC/service-token docs | Validate Keycloak issuer/audience, token lifetime and role mapping on the pilot host. |
| Secrets and logging | pass in code | redactor tests, protected env/rotation runbook | Put all real credentials in the approved secret store; perform I14.5 external rotation rehearsal. |
| Paths, Git and publication | pass in code | path/symlink, worktree, publication and reviewer tests | Keep session storage exclusive to the application account. |
| Worker/sandbox isolation | conditional pass | Rootless unit, no-network worker, security profile artifacts | Run I14.2 audit and verify installed AppArmor/seccomp on Debian 13. |
| Network boundary | conditional pass | nftables/Caddy/Compose artifacts and tests | Apply I14.3 on the intended host and verify from an external network. |
| Data storage, backup and retention | conditional pass | restore rehearsal and bounded retention artifacts | Execute a protected-snapshot restore rehearsal and retain its aggregate evidence. |
| Availability and capacity | open evidence | k6 scenarios, limits and monitoring rules | Execute I14.9 in an isolated stack; tune limits from measured p95/p99 and resource data. |
| Supply chain | pass in repository | CI workflow, Dependabot and SBOM artifacts | Confirm the GitHub Actions workflow completes successfully after push. |
| Attachments and external integrations | conditional pass | strict metadata parsing and narrow outbound adapter | No attachment content download/execution; verify Zoho/n8n/Open WebUI routing and credentials in a controlled pilot. |

## Pilot gates

Do not enable a real project, real vault write path or production publication until all conditional/open evidence above is recorded. Maintain the default-deny policies, no automatic merge, network-disabled sandbox and reviewer gates throughout the pilot. Any new model capability, network allowlist, source mount or write path requires a threat-model amendment and security review.