# Projektmanifest

Das Projektmanifest liegt im Zielrepository unter `.patchpony/project.yaml`.
Es beschreibt ausschließlich die Identität des Projekts, das registrierte
Repository und Pfadgrenzen. Secrets, Tokens, Shell-Kommandos, Runner-Images,
Skills und Knowledge-Quellen gehören nicht in dieses Format.

Die normative Definition ist das JSON Schema
[`schemas/patchpony-project.schema.json`](../schemas/patchpony-project.schema.json).
I3.2 implementiert anschließend die strikte YAML-Deserialisierung und die
Schema-Validierung.

## Beispiel

```yaml
schemaVersion: 1
project:
  id: patchpony
  displayName: PatchPony
repository:
  remoteUrl: https://github.com/Edakuban/PatchPony.git
  defaultBranch: main
paths:
  readable:
    - src/**
    - tests/**
    - docs/**
    - README.md
    - .patchpony/skills/**
  writable: []
  forbidden:
    - .git/**
    - .env
    - .env.*
    - '**/.env'
    - '**/.env.*'
```

## Regeln

- `schemaVersion` ist aktuell immer `1`.
- `project.id` ist stabil, nur klein geschrieben und kein frei formulierter
  Anzeigename.
- `repository.remoteUrl` akzeptiert nur kanonische HTTPS- oder SSH-URLs;
  lokale Pfade sind ausgeschlossen.
- Alle Pfade sind relative, mit `/` geschriebene Globs. Absolute Pfade,
  Backslashes und `..`-Segmente sind verboten.
- `readable`, `writable` und `forbidden` müssen explizit angegeben werden.
  In I3 bleibt der Checkout technisch read-only; `writable` reserviert nur die
  spätere Konfiguration.
- Unbekannte Felder sind nicht erlaubt. Erweiterungen erfolgen als neue
  `schemaVersion`, nicht durch stillschweigend tolerierte Eigenschaften.

Das Manifest ist Konfiguration, keine Berechtigungserweiterung: Kanonische Pfad-, Symlink- und Policy-Prüfungen schränken Zugriffe zusätzlich ein.
