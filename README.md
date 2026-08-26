# PatchPony

![PatchPony logo](https://github.com/Edakuban/PatchPony/blob/main/PatchPony-logo.png)

PatchPony is a security-first internal runtime for AI-assisted project knowledge. It gives Open WebUI and n8n a controlled way to research approved repositories, return verifiable answers, and prepare later reviewable workflows—without giving an LLM a shell, host access, Git credentials, or permission to merge branches.

> **Current state:** a local, read-only multi-project pilot is running. The first controlled write, branch, merge-request, and knowledge-vault publication workflows exist as platform capabilities, but are not enabled through the Open WebUI knowledge chat.

## What works today

In the local pilot, users can ask the **PatchPony · Knowledge** model questions about approved repositories. PatchPony can:

- list the projects available to the n8n service account with `projects.list`;
- switch research between approved projects during a chat while keeping the Gateway allowlist as the hard boundary;
- search only manifest-approved source and documentation paths with `source.search`;
- read bounded excerpts with `source.read`;
- answer in German from verified excerpts and attach project, path, and line citations;
- render source citations as direct GitHub/GitLab links where the repository URL supports it;
- return a clear limitation instead of inventing information when no approved source establishes an answer.

The current local source catalog contains:

| Project ID | Project | Purpose |
| --- | --- | --- |
| `patchpony` | PatchPony | Runtime, architecture, policies, and integration documentation |
| `vocavid` | VocaVid | Desktop application, ComfyUI integration, workflows, and documentation |
| `one-data` | one-data | Application source, API, web frontend, database package, and documentation |

Only server-mounted paths that are explicitly allowed by each project manifest are searchable. `.env` files, Git metadata, credentials, databases, and unmounted paths remain unavailable.

## Architecture

```text
Open WebUI Pipe ──HTTPS──> n8n ──MCP──> PatchPony Gateway
       │                    │                 │
       │                    │                 ├── approved, read-only project sources
       │                    │                 ├── PostgreSQL (runtime metadata and state)
       │                    │                 └── signed handoff ──> Worker / sandbox
       │                    │
       │                    ├── fast model: tool routing and source retrieval
       │                    └── stronger model: final grounded answer
       │
       └── final answer, verified citations, and safe error messages only
```

- **Open WebUI Pipe** sends a minimal, pseudonymized request to n8n; it never receives provider or Gateway credentials.
- **n8n** owns prompt orchestration and the model-provider credentials. The practical local setup uses `gpt-oss:20b` for MCP tool routing and `gpt-oss:120b` for the final answer.
- **Gateway** authenticates requests, enforces scopes and project claims, exposes MCP/REST contracts, audits access decisions, and redacts sensitive data.
- **Pilot sources** are local, read-only mounts controlled by per-project manifests.
- **Worker** accepts only signed, bounded handoffs and runs catalogued tests in a restricted sandbox. It never accepts arbitrary commands.
- **Caddy** terminates local TLS; the Gateway itself is not directly published.

For the component and trust-boundary description, see [docs/architecture.md](docs/architecture.md).

## Security model

PatchPony is deliberately built around technical limits rather than prompt-only promises:

- default-deny authentication, scopes, project claims, tool names, parameters, paths, and source mounts;
- a distinct n8n service token; local development-password mode only in `Development`; OIDC/JWT for later user authentication;
- source access is intersection-based: a project must be mounted, manifest-approved, and included in the service-account allowlist;
- no shell, arbitrary HTTP, Git, plugin, host, or write tools in the read-only knowledge workflow;
- no secrets in Git, logs, source mounts, citations, or audit output;
- human review before publication; no automatic protected-branch merge;
- bounded requests, rate limits, correlation IDs, and safe user-facing failure messages.

See [docs/authentication.md](docs/authentication.md), [docs/pilot-source-mcp.md](docs/pilot-source-mcp.md), and [docs/threat-model.md](docs/threat-model.md).

## Local quick start

### Prerequisites

- Docker Desktop / Docker Engine with Compose
- .NET SDK 10 (for local builds and tests)
- access to an OpenAI-compatible model provider such as `oi.destination.one`

Create a private local environment file:

```powershell
Copy-Item .env.example .env
```

`.env` is ignored by Git. Never commit it or paste its values into chat, tickets, logs, or screenshots.

Set at least:

```text
POSTGRES_PASSWORD=<local database password>
PATCHPONY__AUTH__N8N__TOKEN=<dedicated random n8n service token>
PATCHPONY__AUTH__N8N__PROJECTS=patchpony,vocavid,one-data
PATCHPONY__WORKER__CLAIMSIGNINGKEY=<base64-encoded random 32-byte key>
PATCHPONY_LOCAL_N8N_ENCRYPTION_KEY=<random persistent secret>
PATCHPONY_LOCAL_OWUI_SECRET_KEY=<random persistent secret>
```

Start the complete local test stack:

```powershell
docker compose `
  -f docker-compose.yml `
  -f deploy/development/docker-compose.gateway-debug.yml `
  -f deploy/development/docker-compose.owui-n8n.yml `
  up -d --build
```

- Open WebUI: <http://localhost:3000>
- n8n: <http://localhost:5678>
- Swagger: <https://localhost:8443/swagger>

Swagger uses the local Caddy certificate. Open it in a browser that trusts the generated local root certificate. See [docs/https-reverse-proxy.md](docs/https-reverse-proxy.md).

## Configure the knowledge chat

1. In n8n, import [integrations/n8n/patchpony-read-only-agent.json](integrations/n8n/patchpony-read-only-agent.json).
2. Configure the **PatchPony Service Account** header credential with `X-PatchPony-Service-Token` and the local n8n service token.
3. Configure the destination.one OpenAI-compatible credential.
4. Use a fast model such as `gpt-oss:20b` for **Read-only PatchPony Agent** (tool routing) and `gpt-oss:120b` for **Formulate grounded answer** (final answer). Set the final model token limit to `512` for the pilot.
5. In the MCP client node, select `projects.list`, `source.search`, and `source.read` (plus the existing runtime/knowledge tools if configured).
6. In Open WebUI, import [integrations/open-webui/patchpony_n8n_pipe.py](integrations/open-webui/patchpony_n8n_pipe.py) as an administrator-managed Function and enable it.
7. Set the Pipe valves: `N8N_WEBHOOK_URL`, `N8N_WEBHOOK_TOKEN`, a valid `DEFAULT_PROJECT_ID` as initial context, and `REQUEST_TIMEOUT_SECONDS=120`.

`DEFAULT_PROJECT_ID` is not a lock to one project. It supplies initial context only; for an explicit other project or a cross-project question, the agent calls `projects.list` and can research only a project returned by that tool. The Gateway independently checks the n8n service-account allowlist.

Disable Open WebUI background title, tag, and follow-up generation while **PatchPony · Knowledge** is selected: those prompts are not knowledge requests.

Detailed local setup and troubleshooting: [docs/local-owui-n8n.md](docs/local-owui-n8n.md).

## Example questions

```text
Welche Projekte kann ich aktuell durchsuchen?
Was sind die Kernfeatures von PatchPony?
Wie startet VocaVid lokal und welche Rolle spielt ComfyUI?
Wie ist one-data technisch aufgebaut?
Welche Ports verwendet VocaVid?
```

## What is intentionally not enabled in the chat

The current chat pilot does **not**:

- modify repository files, configuration, or a knowledge vault;
- create branches, commits, pull/merge requests, or merge code;
- execute shell commands, arbitrary tests, or arbitrary HTTP requests;
- access unregistered repositories or user-selected local paths;
- access `.env` files, credentials, Git metadata, or database files.

Those capabilities have separate guarded contracts and require explicit enablement, policies, approval gates, and review before use.

## Develop and verify

```powershell
dotnet restore PatchPony.slnx
dotnet build PatchPony.slnx --no-restore
dotnet test PatchPony.slnx --no-restore
```

## Project documents

- [PLAN.md](PLAN.md) – delivery plan and next work item
- [IMPLEMENTATION-STATUS.md](IMPLEMENTATION-STATUS.md) – verified implementation status
- [docs/architecture.md](docs/architecture.md) – components, data flows, and trust boundaries
- [docs/configuration.md](docs/configuration.md) – configuration and secret handling
- [docs/mcp-tools.md](docs/mcp-tools.md) – MCP contracts
- [docs/openapi.md](docs/openapi.md) – Swagger/OpenAPI access
- [docs/local-owui-n8n.md](docs/local-owui-n8n.md) – end-to-end local setup