# Config-Werkzeuge

I8.10 stellt Config-Validierung und -Patches über REST und MCP bereit. Beide Varianten arbeiten ausschließlich gegen serverseitig konfigurierte Session-Worktrees. Client-Aufrufe enthalten nie einen lokalen oder absoluten Dateipfad.

## REST

- `POST /api/v1/projects/{projectId}/sessions/{sessionId}/config/validate` benötigt `config:read`.
- `POST /api/v1/projects/{projectId}/sessions/{sessionId}/config/patch` benötigt `config:write`.

Beide Requests enthalten `path`, `format` (`json`, `yaml`, `xml`) sowie optional `schemaId`. Der Patch-Request enthält zusätzlich `expectedSourceSha256` und die vollständige UTF-8-Ersetzung als `replacementBase64`.

## MCP

- `config.validate` ist read-only und idempotent.
- `config.patch` führt den validierten, hash-gepinnten Patch mit automatischem Rollback aus.

Die Patch-Nutzlast bleibt auf 1 MiB dekodierte UTF-8-Daten begrenzt. Die Tool-Autorisierung prüft Projektclaim, Scope und eine feste Parameterliste, bevor ein Worktree aufgelöst wird.

## Serverkonfiguration

Die Zuordnung wird unter `PatchPony:ConfigSessions:Sessions` hinterlegt. Jeder Eintrag besitzt eine kanonische `sessionId` (GUID im N-Format), `projectId`, `worktreeRoot` und `manifestFile`. Der Katalog lädt das Manifest beim Start, baut daraus die Pfadpolicy und akzeptiert ausschließlich vorhandene Serverpfade.

`config.patch` benötigt `config:write` und zusätzlich `project_role` bzw. `project_roles` im Format `<projekt-id>:config-editor`. Die Berechtigung gilt nur für dieses eine Projekt.