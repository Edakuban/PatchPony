Beispielsweise ein Bug-Workflow:



GitLab Issue

&#x20;    │

&#x20;    ▼

&#x20;   n8n

&#x20;    │

&#x20;    ▼

PatchPony MCP

&#x20;    │

&#x20;    ├─ project.open("backend")

&#x20;    ├─ issue.analyze(...)

&#x20;    ├─ skills.read(...)

&#x20;    ├─ source.search(...)

&#x20;    │

&#x20;    ▼

&#x20;  LLM

&#x20;    │

&#x20;    ▼

PatchPony

&#x20;    ├─ session.create()

&#x20;    ├─ patch.apply()

&#x20;    ├─ test.run()

&#x20;    └─ changes.diff()

&#x20;    │

&#x20;    ▼

&#x20;   n8n

&#x20;    │

&#x20;    ├─ vielleicht Human Approval

&#x20;    │

&#x20;    ▼

PatchPony

&#x20;    ├─ changes.commit()

&#x20;    ├─ git.push()

&#x20;    └─ merge\_request.create()



Das wäre schon ziemlich mächtig.



Open WebUI könnte parallel dasselbe Projekt interaktiv benutzen:



Analysiere Issue #493.



oder:



Schau dir das Auth-Modul an und sag mir, warum Refresh Tokens manchmal ungültig werden.



oder mit Schreibrechten:



Implementiere deinen Vorschlag und erstelle einen MR.



Open WebUI hat dafür inzwischen genau den passenden Remote-MCP-Mechanismus. MCP-Server können zentral vom Admin eingerichtet und Benutzer/Gruppen darauf berechtigt werden.

