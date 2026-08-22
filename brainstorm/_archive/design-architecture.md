1

&#x20;                 MCP Client / Agent

&#x20;                        │

&#x20;                        │ MCP

&#x20;                        ▼

&#x20;               ┌─────────────────┐

&#x20;               │   MCP Gateway   │

&#x20;               │ auth / policy   │

&#x20;               │ audit / limits  │

&#x20;               └────────┬────────┘

&#x20;                        │

&#x20;           ┌────────────┴────────────┐

&#x20;           │        CodeCell         │

&#x20;           │                         │

&#x20;           │  ┌───────────────────┐  │

&#x20;           │  │ project workspace │  │

&#x20;           │  │                   │  │

&#x20;           │  │ src/              │  │

&#x20;           │  │ config/           │  │

&#x20;           │  │ skills/           │  │

&#x20;           │  │ docs/             │  │

&#x20;           │  └───────────────────┘  │

&#x20;           │                         │

&#x20;           │  read / search / edit  │

&#x20;           │  diff / patch / shell  │

&#x20;           │  validate / test       │

&#x20;           └─────────────────────────┘

&#x20;                      │

&#x20;                NO HOST ACCESS

&#x20;                      │

&#x20;             optional network=None









2



n8n ───────────────┐

&#x20;                  │ MCP / HTTPS

Open WebUI ────────┼──────────────► PatchPony

&#x20;                  │

andere Agents ─────┘

&#x20;                          │

&#x20;                          ▼

&#x20;                   ┌─────────────┐

&#x20;                   │ Project Cell│

&#x20;                   │             │

&#x20;                   │ repo clone  │

&#x20;                   │ skills      │

&#x20;                   │ configs     │

&#x20;                   │ sandbox     │

&#x20;                   │ shell       │

&#x20;                   └──────┬──────┘

&#x20;                          │

&#x20;                          ▼

&#x20;                   GitHub / GitLab

&#x20;                    Bot / Service

&#x20;                      Account



Das passt technisch ziemlich gut zum aktuellen Ökosystem: Open WebUI unterstützt inzwischen native Remote-MCP-Server über Streamable HTTP, inklusive Bearer/OAuth-Authentifizierung und Zugriffskontrolle.



Und genau deshalb würde ich PatchPony von Anfang an HTTP-first machen. stdio höchstens zusätzlich fürs lokale Entwickeln.



GitHub/GitLab würde ich nicht direkt „ins Projekt mounten“



Stattdessen sollte eine Cell einen eigenen Checkout besitzen:



/patchpony/projects/foo/

&#x20;   repo/

&#x20;   .patchpony/



Beim Erstellen/Refresh:



GitLab/GitHub

&#x20;    │

&#x20;    │ clone/fetch

&#x20;    ▼

base checkout

&#x20;    │

&#x20;    ├── session abc (COW/worktree)

&#x20;    ├── session def (COW/worktree)

&#x20;    └── session ghi (COW/worktree)



Ein Agent arbeitet immer auf einer Session/Branch/Worktree, niemals direkt auf main.



Am Ende könnte PatchPony explizite Aktionen anbieten:



git.status

git.diff

git.commit

git.push

git.create\_branch

git.create\_merge\_request



Ich würde git.push und insbesondere MR/PR-Erstellung dabei nicht über beliebiges shell.exec laufen lassen, sondern als eigene privilegierte Capabilities.



Dann kannst du z. B. festlegen:



permissions:

&#x20; inspect: true

&#x20; modify: true

&#x20; execute: true





&#x20; git:

&#x20;   commit: true

&#x20;   push: true

&#x20;   create\_merge\_request: true





&#x20;   force\_push: false

&#x20;   push\_protected\_branch: false



Das ist wesentlich sicherer.

