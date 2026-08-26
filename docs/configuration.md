# Configuration reference

Copy `.env.example` to `.env` for local use. `.env` is intentionally ignored by Git. Deployment systems must provide the same values through a secret manager or protected environment variables.

## Required for Docker Compose

| Variable | Purpose | Secret |
| --- | --- | --- |
| `POSTGRES_PASSWORD` | PostgreSQL password for the local stack. | Yes |
| `PATCHPONY_AUTH__N8N__TOKEN` | Dedicated service credential accepted only from n8n. | Yes |
| `PATCHPONY_WORKER__CLAIMSIGNINGKEY` | Base64-encoded, random 32-byte HMAC key for worker claims. | Yes |

`PATCHPONY_WORKER__CLAIMSIGNINGKEY` must be independent of every other secret. Generate it locally, for example:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

## Local development authentication

| Variable | Purpose |
| --- | --- |
| `PATCHPONY_AUTH__DEVELOPMENTPASSWORD` | Enables the development-only `X-PatchPony-Development-Password` header. |
| `PATCHPONY_AUTH__DEVELOPMENTPROJECTS` | Comma-separated project IDs available to the development identity. |
| `PATCHPONY_AUTH__DEVELOPMENTCONFIGEDITORPROJECTS` | Explicit projects where the local development identity may exercise config-editor checks. |

Use this mode only with `ASPNETCORE_ENVIRONMENT=Development`. It is not a substitute for OIDC.

## Production user authentication

| Variable | Purpose |
| --- | --- |
| `PATCHPONY_AUTH__OIDC__AUTHORITY` | OIDC issuer/authority, e.g. Keycloak at `id.destination.one`. |
| `PATCHPONY_AUTH__OIDC__AUDIENCE` | Audience expected by the PatchPony API. |
| `PATCHPONY_AUTH__OIDC__REQUIREHTTPSMETADATA` | Keep `true` outside exceptional local testing. |

JWTs must carry the appropriate role/scope claims and an exact `project` or `projects` claim. There is no project wildcard.

## n8n, Open WebUI and model provider

| Variable | Purpose | Secret |
| --- | --- | --- |
| `PATCHPONY_AUTH__N8N__TOKEN` | n8n → Gateway service token. | Yes |
| `PATCHPONY_AUTH__N8N__PROJECTS` | Comma-separated n8n project allowlist. | No |
| `PATCHPONY_AI__ENDPOINT` | OpenAI-compatible model-provider endpoint. | No |
| `PATCHPONY_AI__APIKEY` | Model-provider API key. | Yes |
| `PATCHPONY_AI__MODEL` | Model identifier; pilot default: `gpt-oss:20b`. | No |

The Open WebUI Pipe has its own Valve configuration (`N8N_WEBHOOK_URL`, `N8N_WEBHOOK_TOKEN`, `DEFAULT_PROJECT_ID`) inside Open WebUI. Do not put those Pipe values into the PatchPony repository. See [open-webui-pipe.md](open-webui-pipe.md).

## Network and operational limits

| Variable | Default | Purpose |
| --- | --- | --- |
| `GATEWAY_HTTPS_PORT` | `8443` | Published Caddy HTTPS port in local Compose. |
| `PATCHPONY_RATELIMITING__APIPERMITLIMIT` | `60` | Per-subject API permits per window. |
| `PATCHPONY_RATELIMITING__MCPPERMITLIMIT` | `30` | Per-subject MCP permits per window. |
| `PATCHPONY_RATELIMITING__WINDOWSECONDS` | `60` | Fixed-window duration. |
| `PATCHPONY_RATELIMITING__MAXIMUMTRACKEDPARTITIONS` | `1000` | Bound on in-memory rate-limit partitions. |
| `PATCHPONY_CORS__ALLOWEDORIGINS__0` | unset | Optional exact trusted browser origin; CORS is otherwise disabled. |

## Zoho Projects task integration

| Variable | Purpose | Secret |
| --- | --- | --- |
| `PATCHPONY_ZOHO__WEBHOOKSECRET` | Validates Zoho webhook signatures. | Yes |
| `PATCHPONY_ZOHO__APIBASEURI` | Zoho Projects API base URI. | No |
| `PATCHPONY_ZOHO__PORTALID` | Zoho portal identifier (current internal portal: `hubermedia`). | No |
| `PATCHPONY_ZOHO__ACCESSTOKEN` | OAuth token for task comments. | Yes |

PatchPony treats Zoho records as tasks, not GitHub issues. The custom `channel` field determines whether a task is a generic task, feature request, bug or change request.

## Source and vault registration

Pilot source mounts are configured server-side in `docker-compose.yml` and `integrations/pilot-projects/`. Do not mount `.git`, `.env`, credentials, a user home directory or a broad workspace root. Knowledge-vault registration is intentionally not enabled yet; when it is, it must use the source/project path-access contract in [knowledge-source-access.md](knowledge-source-access.md).