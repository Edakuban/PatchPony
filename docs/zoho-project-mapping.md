# Zoho-Projektzuordnung

Die Projektwahl kommt ausschließlich aus `PatchPony__Zoho__ProjectMappings`. Jede Regel enthält die numerische Zoho-Projekt-ID und eine lokale `ProjectManifestId`.

```text
PatchPony__Zoho__ProjectMappings__0__ZohoProjectId=1362699000013318565
PatchPony__Zoho__ProjectMappings__0__ProjectManifestId=patchpony
```

Der signierte Webhook muss `task.projectId` liefern. PatchPony akzeptiert genau eine exakte Zuordnung. Fehlende, ungültige, unbekannte oder mehrdeutige Mapping-Regeln stoppen den Workflow fail-closed; Task-Titel, URL und Kanal werden nie zur Projektwahl verwendet.