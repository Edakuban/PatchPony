# n8n-Credential: PatchPony Service Account

Diese Einrichtung gilt für `n8n.oi.destination.one`. Das Credential ist ausschließlich für n8n → PatchPony bestimmt; es ist weder ein Open-WebUI- noch ein Benutzercredential.

## Secret bereitstellen

1. Einen neuen zufälligen Secret-Wert erzeugen und **denselben** Wert ausschließlich in den geschützten Umgebungen von PatchPony und n8n ablegen.
2. In der PatchPony-Deploy-Umgebung setzen:

   ```text
   PATCHPONY__AUTH__N8N__TOKEN=<zufälliger-n8n-service-token>
   PATCHPONY__AUTH__N8N__PROJECTS=<kommagetrennte-freigegebene-projekt-ids>
   ```

3. In der geschützten n8n-Umgebung setzen:

   ```text
   PATCHPONY_GATEWAY_URL=https://<oeffentlicher-patchpony-host>
   PATCHPONY_N8N_SERVICE_TOKEN=<derselbe-zufällige-n8n-service-token>
   ```

Der Token steht nie in Workflow-JSON, Notes, Logs oder Git. Lokal wird er nur in der ignorierten `.env` hinterlegt.

## Credential in n8n

In n8n ein Credential vom Typ **Header Auth** anlegen:

| Feld | Wert |
|---|---|
| Name | `PatchPony Service Account` |
| Header Name | `X-PatchPony-Service-Token` |
| Header Value | `{{$env.PATCHPONY_N8N_SERVICE_TOKEN}}` |

Der HTTP-Request-Node verwendet dieses Credential und als Base-URL `{{$env.PATCHPONY_GATEWAY_URL}}`. Das Credential wird ausschließlich an den PatchPony-Host gesendet, nie an Modellprovider, Zoho oder beliebige URLs.

## Berechtigungsgrenze

Der Gateway akzeptiert genau einen Headerwert, vergleicht ihn ohne Klartext-Logging und erstellt ausschließlich `service-n8n`. Ohne freigegebene Projekt-ID oder passenden Read-Scope wird jeder Tool-Aufruf trotz gültigem Token abgelehnt. Der aktuelle Preflight ist:

```text
POST /api/v1/projects/{projectId}/access
```

Damit kann der n8n-Workflow vor einem späteren MCP-Tool-Aufruf die projekt-, tool- und parameterbezogene Freigabe prüfen. Die eigentliche Workflow-Orchestrierung folgt in I6.3.