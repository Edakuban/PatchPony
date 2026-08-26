# Local Open WebUI + n8n test stack

This is an intentionally local, loopback-only end-to-end test setup:

```text
Browser -> Open WebUI -> n8n -> PatchPony Gateway -> approved VocaVid sources
                              |
                              +-> oi.destination.one / gpt-oss:120b
```

Open WebUI and n8n are not exposed to the LAN. The Gateway is only reachable from n8n on the private Docker `edge` network; its local diagnostic port is optional and remains loopback-only.

## 1. Local secrets

Add these two independent random values to the local `.env` (never commit it):

```dotenv
PATCHPONY_LOCAL_N8N_ENCRYPTION_KEY=<random-persistent-secret>
PATCHPONY_LOCAL_OWUI_SECRET_KEY=<random-persistent-secret>
```

They protect local n8n credentials and Open WebUI sessions respectively. Keep both values stable while retaining the Docker volumes. The existing `PATCHPONY__AUTH__N8N__TOKEN` remains the separate n8n-to-PatchPony service credential.

Generate a suitable value locally in PowerShell when needed:

```powershell
$bytes = New-Object byte[] 32; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes); [Convert]::ToBase64String($bytes)
```

## 2. Start the stack

```powershell
docker compose `
  -f docker-compose.yml `
  -f deploy/development/docker-compose.gateway-debug.yml `
  -f deploy/development/docker-compose.owui-n8n.yml `
  up -d --build
```

Open WebUI: `http://localhost:3000`\
n8n: `http://localhost:5678`\
PatchPony Swagger: `https://localhost:8443/swagger`

Create the first local administrator account in each UI. The services use persistent, named Docker volumes, so do not run `down -v` unless deliberately resetting local chats, n8n credentials, workflows, and accounts.

## 3. Configure n8n

1. Import [`integrations/n8n/patchpony-read-only-agent.json`](../integrations/n8n/patchpony-read-only-agent.json).
2. Create **PatchPony Service Account** as an HTTP Header Auth credential:
   - Header: `X-PatchPony-Service-Token`
   - Value: the local `.env` value of `PATCHPONY__AUTH__N8N__TOKEN`
3. Create **destination.one OpenAI-compatible** with the destination.one OpenAI-compatible endpoint and API key; select `gpt-oss:120b`.
4. Generate a third, separate random value and create **Open WebUI PatchPony Webhook (X-PatchPony-Webhook-Token)**:
   - Header: `X-PatchPony-Webhook-Token`
   - Value: this newly generated webhook secret
5. In the MCP client select only `projects.list`, `source.search`, and `source.read` for the current repository pilot. Do not select `knowledge.*` until a real knowledge vault is registered.
6. Activate the imported workflow.

The overlay already supplies `PATCHPONY_GATEWAY_URL=http://gateway:8080`, so n8n talks to PatchPony privately and does not need to trust the local Caddy certificate. It enables n8n expression access only for this local stack because the imported MCP node resolves that one address through `$env`; never carry that setting into a shared or production n8n instance.

## 4. Configure Open WebUI

1. Log in as the local admin, then open **Workspace -> Functions**.
2. Import [`integrations/open-webui/patchpony_n8n_pipe.py`](../integrations/open-webui/patchpony_n8n_pipe.py), enable it, and open its valves.
3. Set:

   ```text
   N8N_WEBHOOK_URL=http://n8n:5678/webhook/patchpony/read-only
   N8N_WEBHOOK_TOKEN=<the separate webhook secret from step 3.4>
   DEFAULT_PROJECT_ID=vocavid
   ```

4. In the model selector choose **PatchPony · Knowledge** and ask, for example:

   ```text
   How does VocaVid use ComfyUI, and how is a local project started?
   ```

The Pipe is the selected Open WebUI model. The language model is deliberately configured in n8n, where its credentials stay; Open WebUI receives only the final answer and verified source citations.

## Verified local flow

The imported workflow uses two steps: a bounded read-only MCP retrieval agent, then a separate tool-free model call that writes the final answer from verified excerpts. Set **Maximum Number of Tokens** to `512` on the answer model.

Disable Open WebUI title, tag, and follow-up generation while PatchPony is selected; these are internal background prompts, not knowledge questions.

Smoke tests:

1. Ask how VocaVid starts locally and what ComfyUI does.
2. Ask which port VocaVid uses.
3. Verify a direct answer, source citations, and clickable Git links.
4. Ask for an unverified fact; the answer must say that approved sources do not establish it.

## Troubleshooting

- **Pipe says it is not fully configured:** check all three valves, especially that `N8N_WEBHOOK_URL` uses `n8n`, not `localhost`. `localhost` inside the Open WebUI container would refer to Open WebUI itself.
- **n8n cannot call PatchPony:** verify `PATCHPONY_GATEWAY_URL` remains `http://gateway:8080` and the `PatchPony Service Account` uses the current `PATCHPONY__AUTH__N8N__TOKEN`.
- **No answer from the agent:** inspect the execution in local n8n first. The workflow deliberately suppresses internal errors from the chat response.
- **Need a clean reset:** stop first, then run `docker compose ... down -v` only if deleting all local OWUI/n8n data is intended.