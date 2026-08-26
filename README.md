# PatchPony

![PatchPony logo](https://github.com/Edakuban/PatchPony/blob/main/PatchPony-logo.png)

PatchPony is a security-first internal runtime for AI-assisted software delivery. It gives Open WebUI and n8n a tightly scoped way to work with approved project context, tickets, configuration and knowledge—without giving an LLM a shell, host access, direct Git credentials or permission to merge protected branches.

> **Implementation status:** the platform foundation and many guarded workflows are implemented. It is still an internal pilot: no real production repositories or knowledge vaults are registered by default, and no automatic merge path exists.

## What it does

PatchPony is designed to turn a request into a reviewable, traceable change process:

- exposes constrained MCP and REST contracts for approved, read-only project operations;
- receives and normalizes Zoho Projects tasks (including the `channel` field for generic tasks, feature requests, bugs and change requests);
- enforces project, role, scope, tool and parameter policies before a capability is invoked;
- creates controlled session/worktree, configuration-patch and sandbox-test workflows;
- records audit-safe decisions, correlation IDs and bounded execution results;
- prepares human review and Git publication workflows, while keeping merge approval outside PatchPony.

The knowledge API contracts, link parser, frontmatter validation, attachment allowlist and content guards are already present. A real Obsidian vault remains deliberately unconfigured until it is explicitly approved and mapped to a project.

## Architecture

```text
Open WebUI Pipe ──HTTPS──> n8n ──MCP / REST──> PatchPony Gateway
                                              │
                                              ├── PostgreSQL (metadata and state)
                                              ├── approved read-only source / knowledge contracts
                                              └── signed private handoff ──> Worker
                                                                            │
                                                         controlled Git worktrees / rootless sandbox
```

- **Gateway** – public internal boundary: authentication, authorization, API/MCP contracts, rate limits, redaction and access audit.
- **Core** – domain rules for projects, tickets, sessions, configuration, review and safety policies.
- **Infrastructure** – controlled access to Git, local checkouts, persistence and parsers.
- **Worker** – validates signed handoffs and runs only catalogued tests in a restricted sandbox; it never accepts arbitrary commands.
- **Caddy** – TLS boundary for the local Compose setup; the Gateway itself is not published directly.

For the full component and trust-boundary description, see [docs/architecture.md](docs/architecture.md). API details are linked from [docs/rest-api.md](docs/rest-api.md), [docs/mcp-tools.md](docs/mcp-tools.md) and [docs/openapi.md](docs/openapi.md).

## Security model

PatchPony follows default-deny rules throughout:

- authenticated Gateway and MCP endpoints, with a local development-password mode only in `Development`;
- OIDC/JWT for users and a separate n8n service token;
- exact project claims, named scopes and allowlisted tool parameters;
- no secrets in logs, audit entries, source mounts or Git;
- non-root, no-network sandbox containers with pinned runners and catalogued commands;
- human review before publication and no automatic protected-branch merge;
- knowledge content guards for Obsidian plugins/snippets, scripts, executables and non-allowlisted attachments.

The concise operational guide is [docs/authentication.md](docs/authentication.md); further policy documents are indexed in `docs/`.

## Configure locally

### Prerequisites

- .NET SDK 10
- Docker Desktop / Docker Engine with Compose
- optional: a destination.one OpenAI-compatible endpoint for future model-backed flows

Copy the template and keep the resulting file private:

```powershell
Copy-Item .env.example .env
```

`.env` is Git-ignored. Never commit it or paste its values into tickets, logs or chat.

Set at least these values before starting the Compose stack:

```text
POSTGRES_PASSWORD=<local database password>
PATCHPONY_AUTH__N8N__TOKEN=<dedicated random n8n secret>
PATCHPONY_WORKER__CLAIMSIGNINGKEY=<base64-encoded random 32-byte key>
```

For local authenticated API testing, add:

```text
PATCHPONY_AUTH__DEVELOPMENTPASSWORD=<local password>
PATCHPONY_AUTH__DEVELOPMENTPROJECTS=patchpony,vocavid
```

For production, use OIDC instead of the development password:

```text
PATCHPONY_AUTH__OIDC__AUTHORITY=https://id.destination.one/
PATCHPONY_AUTH__OIDC__AUDIENCE=patchpony-api
PATCHPONY_AUTH__OIDC__REQUIREHTTPSMETADATA=true
```

The OpenAI-compatible provider settings are reserved for model-backed functionality; the first intended pilot model is `gpt-oss:20b` at destination.one:

```text
PATCHPONY_AI__ENDPOINT=https://oi.destination.one/
PATCHPONY_AI__APIKEY=<provider key>
PATCHPONY_AI__MODEL=gpt-oss:20b
```

See [docs/configuration.md](docs/configuration.md) for the complete variable reference, secret handling and integration-specific configuration.

## Run and verify

```powershell
dotnet restore PatchPony.slnx
dotnet build PatchPony.slnx --no-restore
dotnet test PatchPony.slnx --no-restore
```

Start the local stack:

```powershell
docker compose up --build
```

Caddy exposes the local HTTPS endpoint at `https://localhost:8443` by default. See [docs/https-reverse-proxy.md](docs/https-reverse-proxy.md) for local certificates and production deployment boundaries.

## Open WebUI and n8n pilot

The repository includes an importable Open WebUI Pipe. It forwards only the final user question to n8n; n8n owns orchestration, prompts and MCP calls to PatchPony. Installation and the minimal request/response contract are documented in [docs/open-webui-pipe.md](docs/open-webui-pipe.md).

The first two locally mounted, read-only pilot sources are PatchPony and VocaVid. They are intentionally test-only; real repositories must be explicitly registered later. Details: [docs/pilot-sources.md](docs/pilot-sources.md).

## Project documents

- [PLAN.md](PLAN.md) – delivery plan and next work item
- [IMPLEMENTATION-STATUS.md](IMPLEMENTATION-STATUS.md) – verified implementation status
- [docs/architecture.md](docs/architecture.md) – components, data flows and trust boundaries
- [docs/configuration.md](docs/configuration.md) – environment configuration reference
- [docs/knowledge-contracts.md](docs/knowledge-contracts.md) – knowledge API and safety contracts
- [docs/local-development.md](docs/local-development.md) – local development notes