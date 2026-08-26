# Architecture

## Components

| Component | Responsibility | Trust boundary |
| --- | --- | --- |
| Open WebUI Pipe | Minimal user-facing pilot adapter; sends the final question only. | Does not receive PatchPony, model-provider or Git credentials. |
| n8n | Orchestrates approved agent prompts and invokes PatchPony MCP/REST capabilities. | Authenticates with its dedicated service token. |
| Gateway | HTTP/MCP ingress, authentication, project/tool policy enforcement, rate limiting, redaction and audit. | Only externally reachable PatchPony application component. |
| Core | Domain contracts and policy decisions. | Has no direct network or shell capability. |
| Infrastructure | Controlled persistence, parsers, source/vault and Git adapters. | Adapters are server-composed, never chosen from request input. |
| Worker | Receives signed short-lived claims and executes registered sandbox jobs. | Private handoff only; no public execution API. |
| PostgreSQL | Persistent operational metadata. | No credentials or raw source material should be stored in audit records. |
| Caddy | TLS termination and local HTTPS entry point. | Gateway remains internal to the Compose network. |

## Trust flow

1. A user asks a question through the Open WebUI Pipe.
2. The Pipe sends a minimal payload to n8n over HTTPS.
3. n8n authenticates to the Gateway using `X-PatchPony-Service-Token` and calls only known MCP/REST contracts.
4. The Gateway authenticates, derives read-only scopes, validates the exact project, tool and bounded parameters, then creates an audit-safe decision.
5. Where a job requires execution, the Gateway creates a signed, expiring worker claim. The Worker accepts it only for its own worker ID and only for a catalogued command/runner.
6. Results are bounded and stored as metadata; a human remains responsible for review and approval before any publication.

## Deliberate non-capabilities

- No LLM gets direct shell, Docker socket or arbitrary network access.
- No API chooses a filesystem path, Docker image, executable, argument list or schema from user input.
- No production source repository or Obsidian vault is configured automatically.
- No automatic merge into protected branches exists.

## Knowledge boundary

Knowledge contracts are stable but unavailable until a source is explicitly registered and assigned to a project. The later source adapter must respect source/project path policies, parser limits, attachment allowlists and the content-deny policy. See [knowledge-contracts.md](knowledge-contracts.md) and [knowledge-source-access.md](knowledge-source-access.md).