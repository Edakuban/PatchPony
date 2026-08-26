# Knowledge-Source und projektbezogene Vault-Pfade

I13.1 trennt ein Git-versioniertes Knowledge-Vault technisch vom Projekt-Repository und ergänzt dafür eine eigene, default-deny Zugriffsschicht. Die bestehende `KnowledgeSource`-Registrierung beschreibt ausschließlich Remote, Branch und stabile Source-ID; `KnowledgeSourceProjectAccess` ordnet dieser Source anschließend projektbezogene Vault-Pfadfreigaben zu.

```text
Source:     vault-remote + default branch
Projekt:    PatchPony
Read:       teams/patchpony/**, shared/*.md
Write:      teams/patchpony/drafts/**
```

Read und Write sind unabhängige Allowlisten. Absolute Pfade, Backslashes und `..` werden abgelehnt. Ohne genau passende Kombination aus Source-ID und Projekt-ID ist jeder Zugriff gesperrt.

Es wurde kein Vault registriert, geklont oder gelesen. Die konkrete Source-Registrierung und Pfadkonfiguration erfolgt erst mit echten, freigegebenen Daten; I13.1 liefert dafür lediglich den getesteten Core-Vertrag.