# Open WebUI → n8n Knowledge Pipe

[`integrations/open-webui/patchpony_n8n_pipe.py`](../integrations/open-webui/patchpony_n8n_pipe.py) is an administrator-managed Open WebUI Pipe Function. It appears in the model chooser as **PatchPony · Knowledge** and forwards only a bounded, minimal request to n8n.

## Install and configure

1. In Open WebUI, open **Workspace → Functions** as an administrator and import the complete Pipe source.
2. Enable the function and set `N8N_WEBHOOK_URL`, `N8N_WEBHOOK_TOKEN`, `DEFAULT_PROJECT_ID`, and `REQUEST_TIMEOUT_SECONDS=120` in its valves. Keep the Pipe timeout at least as high as the n8n MCP client timeout for multi-project research.
3. Select **PatchPony · Knowledge** in a chat.

`N8N_WEBHOOK_TOKEN` is a separate random secret, sent only to the configured trusted n8n webhook. `DEFAULT_PROJECT_ID` is an administrator-controlled initial project context and must also be included in the n8n service account allowlist. It does not grant access by itself: a cross-project lookup requires `projects.list`, and the Gateway only permits a project explicitly allowed for the n8n service account.

## Supported interactions

Regular input is classified as `knowledge-question`: the n8n agent may use only the selected read-only Source and Knowledge MCP tools and answers from available, approved evidence.

For a documentation-maintenance request, start the message exactly with:

```text
/knowledge-maintain Describe the desired documentation update
```

This is classified as `knowledge-maintenance`. The agent may research using the same read-only tools, but returns only a compact **draft proposal** with target page, rationale, and suggested semantic operations. It does not write a file, create a session, create a branch, commit, push, create a merge request, or merge anything. A later, explicit reviewed workflow is required for those actions. An empty maintenance request is rejected by the Pipe before it reaches n8n.

## Request and response contract

The Pipe sends only the following data to n8n:

```json
{
  "requestId": "UUID",
  "source": "open-webui",
  "projectId": "administrator-bound project",
  "requester": { "source": "open-webui", "subject": "owui-sha256:…" },
  "intent": "knowledge-question | knowledge-maintenance",
  "question": "the request text"
}
```

n8n must respond synchronously with:

```json
{
  "answer": "Developer-facing answer or explicitly labelled proposal",
  "sources": [{ "projectId": "patchpony", "path": "README.md", "startLine": 1, "endLine": 3 }],
  "toolCalls": ["knowledge.read"]
}
```

The Pipe never forwards the full chat history, clear-text Open WebUI user attributes, model-provider keys, or gateway/n8n credentials. It pseudonymizes the Open WebUI subject with SHA-256. Only verified citations and allowed tool names are rendered beneath the answer. Missing configuration, timeouts, cancellation, rate limits, and upstream failures use safe user-facing messages without infrastructure details.

Open WebUI functions run with the Open WebUI process permissions. Import and review this Pipe only as an administrator and configure only a trusted HTTPS n8n URL. The n8n artifact is [`integrations/n8n/patchpony-read-only-agent.json`](../integrations/n8n/patchpony-read-only-agent.json); despite its retained filename for backwards-compatible imports, it is named **PatchPony · Knowledge Agent** inside n8n.