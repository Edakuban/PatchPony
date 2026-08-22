# PatchPony – Architektur- und Umsetzungskonzept

> Der schrittweise Umsetzungsfahrplan befindet sich im [Projekt- und Iterationsplan](../PLAN.md).

## 1. Kurzfassung

PatchPony soll Entwicklungsprojekte sicher und standardisiert für LLM-Agenten zugänglich machen. Ein Agent bekommt keinen direkten Zugriff auf den Host oder einen produktiven Checkout. Stattdessen arbeitet er in einer zeitlich begrenzten Session mit einem eigenen Git-Worktree beziehungsweise einer Copy-on-Write-Arbeitskopie.

Die primären Anwendungsfälle sind:

1. Bugreports aus Zoho Projects prüfen, analysieren und optional als Merge Request vorbereiten.
2. Feature- und Change-Requests bearbeiten, insbesondere klar begrenzte Änderungen an Konfigurationsdateien.
3. Entwickler über Open WebUI bei Codesuche, Architekturfragen und Coprogramming unterstützen.
4. Einen Git-versionierten Obsidian-Vault als gemeinsames „Second Brain“ für Modul- und Firmenwissen lesen und kontrolliert über Merge Requests aktualisieren.

PatchPony ist dabei nicht einfach „ein MCP-Server mit Shell-Zugriff“, sondern eine isolierte Projekt-Runtime. MCP ist eine Schnittstelle dieser Runtime. Für deterministische n8n-Workflows wird zusätzlich eine REST-API angeboten.

Für Version 1 wird folgender Stack empfohlen:

| Bereich | Empfehlung |
|---|---|
| Host-System | Debian 13 minimal |
| Container | Docker Engine und Docker Compose |
| PatchPony-Dienste | C# auf .NET 10 LTS |
| MCP | offizielles MCP C# SDK für ASP.NET Core, Streamable HTTP |
| Workflow-Orchestrierung | n8n |
| Interaktive Oberfläche | Open WebUI mit Pipe zu n8n |
| Persistenter Zustand | PostgreSQL |
| TLS / Reverse Proxy | Caddy oder Traefik |
| Versionsverwaltung | GitHub oder GitLab über einen eingeschränkten Bot |
| Wissensbasis | Git-versionierter Obsidian-Vault aus Markdown-Dateien |
| Beobachtbarkeit | strukturierte Logs und OpenTelemetry |

Die Sicherheitsregel für Version 1 lautet:

> PatchPony darf Änderungen automatisch vorbereiten, validieren, committen und als Merge Request bereitstellen. Es führt jedoch keine automatischen Merges in geschützte Branches durch.

---

## 2. Zielbild

PatchPony stellt jedem Agenten ein standardisiertes und isoliertes Projekt-Environment bereit. Dieses Environment enthält nur die Daten und Fähigkeiten, die für den jeweiligen Auftrag erforderlich sind:

- Projektbeschreibung und projektspezifische Skills
- freigegebene Quellcodebereiche
- freigegebene Konfigurationsdateien
- relevante Dokumentation und freigegebene Bereiche des Wissensvaults
- definierte Prüf- und Testbefehle
- eine temporäre Arbeitskopie für Änderungen

Der Agent arbeitet nie direkt auf `main`, `master` oder einem anderen geschützten Branch.

### 2.1 Ziele

- Tickets automatisch auf Vollständigkeit und Umsetzbarkeit prüfen
- relevanten Code und relevante Konfiguration zuverlässig finden
- Modul- und Firmenwissen projektübergreifend auffindbar und nutzbar machen
- strukturierte Implementierungspläne erzeugen
- kleine, klar eingegrenzte Änderungen vorbereiten
- Tests und Validatoren reproduzierbar ausführen
- jeden Zugriff und jede Änderung auditierbar machen
- mehreren Clients dieselbe sichere Runtime anbieten
- projektbezogene Regeln statt globaler Agenten-Prompts verwenden

### 2.2 Nicht-Ziele für Version 1

- vollautonome Änderungen ohne menschliche Review
- automatisches Mergen in geschützte Branches
- freier Shell-Zugriff für das Modell
- beliebiger Netzwerkzugriff aus Projekt-Sandboxes
- Ausführen beliebiger Dockerfiles aus fremden Repositories
- Zugriff auf Host-Verzeichnisse, Benutzerprofile oder lokale Credentials
- Ersatz für CI/CD, Issue-Tracker oder GitHub/GitLab

---

## 3. Gesamtarchitektur

```text
Zoho Projects                         Entwickler
      │                                   │
      │ Webhook                           │ Open WebUI
      ▼                                   ▼
     n8n ◄──────── Open WebUI Pipe ───────┘
      │
      │ REST oder MCP über HTTPS
      ▼
┌──────────────────────────────────────────────┐
│ PatchPony Gateway                             │
│                                             │
│ Authentifizierung · Autorisierung · Policy  │
│ MCP · REST · Rate Limits · Audit            │
└──────────────────────┬───────────────────────┘
                       │ internes Protokoll
                       ▼
┌──────────────────────────────────────────────┐
│ PatchPony Worker                              │
│                                             │
│ Sessions · Git-Worktrees · Container        │
│ Config-Validierung · Tests · Diff · Cleanup │
└───────────────┬──────────────────┬───────────┘
                │                  │
                ▼                  ▼
       temporäre Sandbox      GitHub / GitLab
       ohne Host-Zugriff      Bot / Service Account
```

Alle Komponenten können auf derselben dedizierten Linux-Maschine laufen. Gateway, Worker und Projekt-Sandboxes bleiben trotzdem getrennte Container beziehungsweise Sicherheitszonen.

### 3.1 Verantwortlichkeiten

#### Open WebUI

- interaktive Oberfläche für Entwickler
- Auswahl des Modells
- Übergabe der Benutzeranfrage an die Pipe
- Darstellung von Antworten, Plänen, Diffs und Testergebnissen

#### n8n

- Empfangen und Normalisieren von Zoho-Webhooks
- Ausführen des Ticket-Workflows
- LLM-Orchestrierung für Triage und Planung
- Warten auf Freigaben
- Kommentieren und Aktualisieren von Zoho-Tickets
- Aufruf der deterministischen PatchPony-Aktionen

n8n ist die Workflow-Engine, aber keine Sicherheitsgrenze. PatchPony muss jede angeforderte Aktion selbst autorisieren und validieren.

#### PatchPony Gateway

- Remote-MCP über Streamable HTTP
- REST-API für n8n
- Authentifizierung von Benutzern und Service-Accounts
- Rollen- und Tool-basierte Autorisierung
- Validierung aller Eingaben
- Rate Limiting und Request-Limits
- Erzeugen vollständiger Audit-Ereignisse
- Weitergabe ausschließlich strukturierter Aufträge an den Worker

Der Gateway-Container erhält keinen Docker-Socket, keine Git-Credentials und keinen direkten Zugriff auf Projekt-Worktrees.

#### PatchPony Worker

- Klonen und Aktualisieren freigegebener Repositories
- Erstellen und Löschen von Sessions und Worktrees
- Starten gehärteter Projekt-Sandboxes
- kontrollierte Datei-, Config- und Git-Operationen
- kontrollierte Markdown-, Frontmatter- und Link-Operationen im Wissensvault
- Ausführen freigegebener Validatoren und Tests
- Erzeugen von Diffs, Commits und Merge Requests
- Durchsetzen von Laufzeit-, Ressourcen- und Pfadlimits

Der Worker ist nicht öffentlich erreichbar. Seine höhere Berechtigung wird durch ein enges internes API und Netzwerksegment abgeschirmt.

#### Projekt-Sandbox

- kurzlebiger Container pro Auftrag oder Session
- nur der Session-Worktree ist beschreibbar
- kein Docker-Socket
- keine Host-, SSH-, Cloud- oder Git-Credentials
- standardmäßig kein Netzwerk
- non-root, minimale Capabilities und feste Ressourcenlimits

#### PostgreSQL

- Projekte und Projektversionen
- Jobs und Statusübergänge
- Sessions und Locks
- Webhook-Idempotenz
- Freigaben
- Audit-Metadaten
- Referenzen auf Diffs, Tests und Merge Requests

Repositories selbst müssen nicht in PostgreSQL gesichert werden; sie können erneut geklont werden.

---

## 4. Grundprinzipien

### 4.1 Capability Security

Sicherheit darf nicht davon abhängen, dass ein Modell Anweisungen zuverlässig befolgt. Eine unerlaubte Aktion muss technisch unmöglich oder serverseitig abgelehnt werden.

Beispiele:

- Ein Read-only-Agent besitzt kein Schreib-Tool.
- Ein Config-Agent darf nur Pfade verändern, die im Projektmanifest erlaubt sind.
- Eine Sandbox ohne Netzwerk kann keine Daten an einen externen Server senden.
- Ein Container ohne Secrets kann keine Git- oder Cloud-Credentials auslesen.
- Ein Agent erhält keine Aktion für Force-Push oder Merge auf geschützte Branches.

### 4.2 Semantische Werkzeuge

PatchPony stellt fachlich benannte Operationen bereit. Generische Werkzeuge wie `write_file` und `run_command` lassen sich nur schwer sicher autorisieren.

Bevorzugt:

```text
source.search
source.read
config.read
config.patch
config.validate
tests.run
changes.diff
git.create_merge_request
```

Nicht als öffentliche Agenten-Tools vorgesehen:

```text
filesystem.write_anywhere
shell.exec_arbitrary
docker.run_arbitrary
git.push_arbitrary
```

### 4.3 Session-basierte Änderungen

```text
Repository-Basis
    │ read-only
    ├── Session A / Worktree / Branch
    ├── Session B / Worktree / Branch
    └── Session C / Worktree / Branch
```

Jede Änderung gehört zu einer Session. Eine fehlgeschlagene oder kompromittierte Session kann vollständig verworfen werden, ohne den Basis-Checkout oder eine andere Session zu verändern.

### 4.4 Least Privilege

Rechte werden pro Akteur, Projekt, Session und Tool vergeben. Ein Entwickler-Frageagent braucht andere Fähigkeiten als ein Config-Change-Workflow.

### 4.5 Explizite Zustandsübergänge

Ein Auftrag bewegt sich durch definierte Statuswerte:

```text
received
  → triaging
  → needs_information | ready
  → planning
  → planned
  → implementing
  → validating
  → review_ready | failed
  → committed
  → merge_request_created
  → closed
```

Jeder Übergang ist wiederholbar, auditierbar und mit einer Policy verbunden.

---

## 5. Technologieentscheidungen

### 5.1 Host-System: Debian 13

Empfohlen wird eine dedizierte Maschine oder VM mit Debian 13 minimal.

Gründe:

- stabile und schlanke Server-Distribution
- wenig unnötige vorinstallierte Komponenten
- Unterstützung bis 2030
- offizielle Unterstützung durch Docker Engine
- gute Kompatibilität mit Build-, Git- und Konfigurationswerkzeugen

Ubuntu Server 24.04 LTS ist eine vernünftige Alternative, falls dafür bereits Betriebswissen, Images oder Monitoring vorhanden sind.

### 5.2 Container-Basis

Für Gateway und Worker werden die offiziellen .NET-10-Images von Microsoft verwendet. Ein typischer Multi-Stage-Build basiert auf:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
```

Der Gateway kann später auf ein shellloses Chiseled-Image umgestellt werden. Der Worker benötigt weiterhin Git, CA-Zertifikate und einige Systemwerkzeuge und verwendet deshalb zunächst ein normales ASP.NET-Core-Linux-Image. Beide Prozesse laufen als non-root-Benutzer.

Alpine wird für allgemeine Projekt-Runner nicht empfohlen, da `musl` und native Abhängigkeiten unnötige Kompatibilitätsprobleme verursachen können.

Images werden versionsgenau und für produktive Läufe möglichst per Digest gepinnt. Ein Repository darf nicht automatisch sein eigenes beliebiges Dockerfile als vertrauenswürdigen Runner starten.

### 5.3 Sprache und Runtime: C# auf .NET 10 LTS

C# wird für Gateway, Core und Worker verwendet. .NET 10 ist eine aktive LTS-Version und passt zur vorhandenen Entwicklungserfahrung im Team.

Passende Eigenschaften:

- offizielles, gemeinsam mit Microsoft gepflegtes MCP C# SDK
- direkte ASP.NET-Core-Integration für MCP über HTTP
- ausgereifte Authentifizierung, Autorisierung und Dependency Injection
- gute Unterstützung für HTTP, Background Worker und Nebenläufigkeit
- strukturierte Konfiguration und Logging über `Microsoft.Extensions.*`
- gute Unterstützung für PostgreSQL, OpenTelemetry und Containerbetrieb
- CancellationToken-basierte Timeouts und Prozessabbrüche
- vertraute Sprache für Security-Reviews und langfristige Wartung

Empfohlene Bausteine:

| Zweck | Werkzeug |
|---|---|
| Runtime | .NET 10 LTS |
| MCP | `ModelContextProtocol.AspNetCore` |
| HTTP / REST | ASP.NET Core Minimal APIs |
| Dependency Injection | ASP.NET Core / `Microsoft.Extensions.DependencyInjection` |
| Worker | .NET `BackgroundService` |
| Logs | `Microsoft.Extensions.Logging`, optional Serilog |
| PostgreSQL | Npgsql und Entity Framework Core |
| Migrationen | Entity Framework Core Migrations |
| Tracing/Metriken | OpenTelemetry |
| Containersteuerung | Docker Engine API über eine eng begrenzte Worker-Abstraktion |
| YAML | YamlDotNet mit strikter Deserialisierung |
| Tests | xUnit, Testcontainers for .NET und MCP-Conformance-Tests |

Für gezielte Abfragen kann später zusätzlich Dapper eingesetzt werden. Für Version 1 sollte jedoch kein unnötiger Mix verschiedener Datenzugriffsmuster entstehen.

#### Python als ergänzendes Werkzeug

Python bleibt sinnvoll für:

- Agenten-Evaluation mit historischen Tickets
- Prompt-Experimente
- Datenaufbereitung und einmalige Importskripte
- projektspezifische Code- oder Config-Analysen
- Hilfstools innerhalb einer isolierten Projekt-Sandbox

Python ist in Version 1 kein eigener privilegierter PatchPony-Dienst. So entstehen nicht gleichzeitig mehrere Build-, Dependency- und Deployment-Welten. Python-Tools laufen mit gepinnten Abhängigkeiten und denselben Sandbox- und Netzwerkregeln wie andere Projektwerkzeuge.

### 5.4 MCP und REST

PatchPony bietet zwei Interfaces auf derselben Business-Logik:

- MCP für Open WebUI und andere Agenten
- REST für deterministische n8n-Workflows

MCP läuft über Streamable HTTP. Die konkret unterstützten Protokollversionen werden gegen Open WebUI und n8n per Integrationstest geprüft. Da die MCP-Spezifikation weiterentwickelt wird, darf die Protokollversion nicht ungeprüft auf „latest only“ festgesetzt werden.

### 5.5 Git

Der native Git-CLI wird gegenüber einer vollständigen Git-Neuimplementierung in C# bevorzugt. PatchPony startet Git über `ProcessStartInfo` und befüllt `ArgumentList` mit festen Einzelargumenten. Es werden weder eine Shell noch zusammengesetzte Befehlsstrings verwendet. Prozesse werden über `WaitForExitAsync` und einen `CancellationToken` zeitlich begrenzt.

Beispielprinzip:

```text
erlaubt: git status --porcelain=v1
erlaubt: git diff --no-ext-diff
erlaubt: git commit mit serverseitig erzeugter Message
verboten: vom Modell gelieferter String für sh -c
```

GitHub- und GitLab-Aktionen werden über deren APIs oder kontrollierte Git-Aktionen ausgeführt. Credentials befinden sich ausschließlich beim Worker beziehungsweise in einem dedizierten Secret Store.

### 5.6 Konfigurationswerkzeuge

Je Projekt können folgende Validatoren verwendet werden:

- JSON Schema für JSON und, soweit passend, YAML
- XSD für XML
- `jq` für JSON-Abfragen
- `yq` für YAML-Abfragen
- `xmllint` für XML
- projektspezifische Linter und Testbefehle

Diese Werkzeuge werden über vordefinierte Aktionen aufgerufen. Der Agent liefert Daten und Patch-Vorschläge, aber keine freien Shell-Befehle.

---

## 6. Empfohlene Repository-Struktur

```text
patchpony/
├── PatchPony.sln
├── src/
│   ├── PatchPony.Core/
│   │   ├── Authorization/
│   │   ├── Jobs/
│   │   ├── Policies/
│   │   ├── Projects/
│   │   ├── Sessions/
│   │   └── Tools/
│   ├── PatchPony.Contracts/
│   ├── PatchPony.Infrastructure/
│   │   ├── Audit/
│   │   ├── Configuration/
│   │   ├── Git/
│   │   ├── Persistence/
│   │   └── Sandbox/
│   ├── PatchPony.Gateway/
│   │   ├── Mcp/
│   │   └── Rest/
│   ├── PatchPony.Worker/
│   └── PatchPony.Cli/
├── tests/
│   ├── PatchPony.UnitTests/
│   ├── PatchPony.IntegrationTests/
│   ├── PatchPony.PolicyTests/
│   └── PatchPony.SecurityTests/
├── migrations/
├── deploy/
│   ├── compose.yaml
│   ├── gateway.Dockerfile
│   └── worker.Dockerfile
└── docs/
```

`PatchPony.Core` enthält die transport- und infrastrukturunabhängige Business-Logik. MCP, REST, CLI und Worker verwenden dieselben Policies und Anwendungsfälle. `PatchPony.Gateway` bleibt dünn und enthält keine Sicherheitsentscheidungen, die nicht auch im Core beziehungsweise Worker durchgesetzt werden.

---

## 7. Projektdefinition und Skills

Jedes angebundene Projekt besitzt ein PatchPony-Verzeichnis:

```text
.patchpony/
├── project.yaml
├── policy.yaml
├── skills/
│   ├── architecture.md
│   ├── backend.md
│   ├── frontend.md
│   ├── database.md
│   └── testing.md
└── schemas/
    └── knowledge-frontmatter.schema.json
```

### 7.1 Beispiel für `project.yaml`

```yaml
version: 1
name: billing-module

repository:
  provider: gitlab
  default_branch: main

scope:
  readable:
    - src/billing/**
    - tests/billing/**
    - config/billing/**
    - docs/billing/**
  writable:
    - config/billing/*.yaml
  forbidden:
    - config/billing/production-secrets.yaml
    - src/generated/**

knowledge:
  repository: company-knowledge
  readable:
    - company/**
    - modules/billing/**
  writable:
    - modules/billing/**
  allowed_extensions:
    - .md
    - .png
    - .jpg
  forbidden:
    - .obsidian/plugins/**
    - .obsidian/snippets/**
  frontmatter_schema: .patchpony/schemas/knowledge-frontmatter.schema.json

rules:
  - Never modify generated files.
  - Database migrations live in migrations/.
  - Production secrets must never enter an agent session.

validation:
  config:
    - name: billing-schema
      type: json-schema
      schema: .patchpony/schemas/billing.schema.json
      paths:
        - config/billing/*.yaml
  tests:
    - name: billing-unit-tests
      command_id: test.billing.unit
      timeout: 300s

git:
  branch_prefix: patchpony/
  allow_commit: true
  allow_push: true
  allow_merge_request: true
  allow_force_push: false
  allow_protected_branch_push: false
  allow_merge: false
```

Das Manifest wird serverseitig gegen ein eigenes Schema validiert. Ungültige oder unbekannte Felder führen zu einem Fehler; sie werden nicht stillschweigend ignoriert.

### 7.2 Skill-Dateien

Skills enthalten projektspezifisches Wissen, keine zusätzlichen technischen Berechtigungen.

Geeignete Inhalte:

- Architektur und Modulgrenzen
- Namens- und Codekonventionen
- bekannte Fallstricke
- Regeln für generierte Dateien
- Konfigurationssemantik
- Teststrategie
- Definition of Done
- Zuordnung typischer Ticketarten zu Modulen

Skill-Dateien sind ebenfalls untrusted Input. Sie können einem Agenten Kontext geben, aber keine PatchPony-Policy überschreiben.

### 7.3 Git-versionierter Wissensvault

Der Obsidian-Vault ist eine eigene, Git-versionierte Wissensquelle. Er kann als separates Repository registriert und einem oder mehreren PatchPony-Projekten zugeordnet werden. Freigegeben werden nicht automatisch der gesamte Vault, sondern konkrete Pfade für Firmen-, Produkt- oder Modulwissen.

Skills und Wissensvault erfüllen unterschiedliche Aufgaben:

- Skills beschreiben, wie ein Agent in einem Projekt sinnvoll vorgeht.
- Der Vault enthält von Menschen gepflegtes Fach-, Modul-, Architektur- und Prozesswissen.
- Beide liefern Kontext, aber niemals zusätzliche technische Berechtigungen.
- Vault-Änderungen erfolgen ausschließlich in einer isolierten Session und werden als Diff beziehungsweise Merge Request zur menschlichen Review bereitgestellt.

PatchPony interpretiert Obsidian-Inhalte als Daten. Community-Plugins, Skripte, CSS-Snippets, ausführbare Anhänge oder eingebettete Befehle werden weder geladen noch ausgeführt. Markdown-Links, Wiki-Links, Attachments und YAML-Frontmatter werden kanonisch aufgelöst, begrenzt und validiert.

---

## 8. MCP-Werkzeuge

### 8.1 Projekt und Skills

```text
project.describe
project.tree
skills.list
skills.read
```

### 8.2 Quellcode

```text
source.search
source.read
source.references
```

`source.references` ist optional und kann später über Language-Server-Protokolle erweitert werden.

### 8.3 Wissensvault

```text
knowledge.tree
knowledge.search
knowledge.read
knowledge.links
knowledge.patch
knowledge.validate
```

Die lesenden Werkzeuge arbeiten revisionsgebunden und liefern Pfad sowie Fundstelle. `knowledge.patch` akzeptiert nur serverseitig geprüfte Änderungen an freigegebenen Markdown- und Metadatenpfaden. Freie Dateischreibzugriffe oder die Ausführung von Obsidian-Plugins werden nicht angeboten.

### 8.4 Konfiguration

```text
config.list
config.read
config.patch
config.validate
```

`config.patch` akzeptiert vorzugsweise strukturierte Änderungen oder einen serverseitig geprüften Patch. Der Zielpfad wird gegen das Projektmanifest geprüft.

### 8.5 Sessions und Änderungen

```text
session.create
session.describe
session.close
changes.diff
changes.apply
changes.discard
```

### 8.6 Tests

```text
tests.list
tests.run
tests.result
```

Der Agent wählt eine registrierte Test-ID. Er übergibt keinen beliebigen Befehl.

### 8.7 Git

```text
git.status
git.create_branch
git.commit
git.push
git.create_merge_request
```

Jede Git-Aktion ist separat berechtigbar. Force-Push, Push auf geschützte Branches und Merge sind in Version 1 nicht verfügbar.

### 8.8 Kein generisches Shell-Tool in Version 1

Ein allgemeines `shell.exec` wird zunächst nicht öffentlich exponiert. Falls es später erforderlich wird, muss es eine eigene Hochrisiko-Capability mit zusätzlicher Freigabe, Sandbox und Netzwerkrestriktion sein.

---

## 9. Rollen und Berechtigungen

### 9.1 Beispielrollen

| Rolle | Lesen | Config ändern | Source ändern | Tests | Commit/MR |
|---|---:|---:|---:|---:|---:|
| `code-reader` | ja | nein | nein | nein | nein |
| `issue-planner` | ja | nein | nein | optional read-only | nein |
| `knowledge-reader` | ja | nein | nein | nein | nein |
| `knowledge-editor` | ja | nein | nein | optional Validatoren | ja |
| `config-editor` | ja | ja, erlaubte Pfade | nein | ja | ja |
| `implementation-agent` | ja | ja | optional | ja | ja |
| `reviewer` | ja | nein | nein | ja | Freigabe, kein Merge |

### 9.2 Beispiel-Scopes

```text
project:read
skills:read
knowledge:read
knowledge:write
source:read
config:read
config:write
tests:run
changes:write
git:commit
git:push
git:merge-request
```

Eine positive Tool-Berechtigung reicht nicht aus. Zusätzlich werden Projekt, Session, Pfad, Branch und Parameter geprüft.

---

## 10. Use-Case 1: Bugreport aus Zoho Projects

### 10.1 Ablauf

```text
Zoho Webhook
  → n8n normalisiert Ticket und Anhänge
  → Idempotenzprüfung
  → Projekt und Tickettyp bestimmen
  → Read-only-Triage
  → fehlende Informationen?
       ja  → gezielten Kommentar in Zoho erstellen und beenden
       nein → Skills, Source, Config und Wissensvault untersuchen
  → strukturierten Plan erstellen
  → Automatisierung laut Policy zulässig?
       nein → Plan an Dev-Team
       ja  → Session und Branch erstellen
             → Patch anwenden
             → validieren und testen
             → Diff und Risikobewertung erstellen
             → Commit, Push und Merge Request
             → Dev-Team zur Review informieren
```

### 10.2 Mindestinformationen eines Bugreports

- verständliche Zusammenfassung
- erwartetes Verhalten
- tatsächliches Verhalten
- reproduzierbare Schritte oder aussagekräftige Beobachtungen
- betroffene Version beziehungsweise Umgebung
- Impact oder Priorität
- relevante Logs, Screenshots oder Beispieldaten, soweit erforderlich

Nicht jedes Ticket benötigt jedes Feld. Die benötigten Angaben können projekt- und tickettypspezifisch definiert werden.

### 10.3 Strukturierter Analyse-Output

```yaml
status: ready
summary: "..."
confidence: medium
likely_files:
  - src/Foo/FooService.cs
related_config:
  - config/foo.yaml
tests:
  - test.foo.unit
implementation_plan:
  - "..."
risks:
  - "..."
missing_information: []
automation_recommendation: plan_only
```

`plan.md` kann als gerenderte Darstellung dieses strukturierten Ergebnisses erzeugt werden. Für die Workflow-Logik sollte jedoch ein validierbares JSON- oder YAML-Objekt verwendet werden.

### 10.4 Wann darf automatisch implementiert werden?

Die Entscheidung darf nicht allein darauf beruhen, dass ein Modell eine Änderung als „klein“ bezeichnet.

Eine automatische Vorbereitung ist nur zulässig, wenn alle definierten Bedingungen erfüllt sind, zum Beispiel:

- ausschließlich erlaubte Pfade werden verändert
- keine Secrets-, Produktions- oder Deployment-Dateien sind betroffen
- kein generierter Code wird verändert
- Diff liegt unter einem konfigurierten Größenlimit
- kein neuer externer Dependency-Eintrag
- keine Datenbankmigration
- alle Parser, Schemas, Linter und Tests sind erfolgreich
- keine Policy-Warnung ist vorhanden
- die Änderung erfolgt auf einem neuen Bot-Branch
- das Ergebnis wird ausschließlich als Merge Request bereitgestellt

Bei Unsicherheit wird auf `plan_only` zurückgefallen.

---

## 11. Use-Case 2: Feature- und Change-Requests

Dieser Workflow ähnelt dem Bugreport-Workflow. Config-only-Änderungen eignen sich besonders gut für eine kontrollierte Automatisierung.

Zusätzliche Prüfungen:

- ist der gewünschte Konfigurationsschlüssel bekannt?
- ist der Zielpfad schreibbar?
- stimmt der gewünschte Wert mit Typ, Wertebereich und Schema überein?
- entstehen widersprüchliche Einstellungen?
- betrifft die Änderung mehrere Umgebungen?
- ist für Produktion eine zusätzliche Freigabe notwendig?
- existiert ein projektspezifischer Validator?

Empfohlene Policy:

```text
Development-Konfiguration → MR nach erfolgreicher Validierung
Staging-Konfiguration     → MR plus benannter Reviewer
Produktionskonfiguration  → Plan/MR nur mit expliziter Freigabe
Secrets                   → niemals über den Agenten-Workflow
```

Ein allgemeiner Feature-Request mit Source-Code-Änderungen bleibt in Version 1 zunächst ein Planungs- und Recherche-Workflow. Die automatische Implementation kann später projektweise aktiviert werden.

---

## 12. Use-Case 3: Entwicklerfragen und Coprogramming

Für Entwicklerfragen wird eine Read-only-Rolle verwendet.

Mögliche Fragen:

- Wo wird ein bestimmtes Verhalten implementiert?
- Welche Module hängen von einem Service ab?
- Welche Config steuert eine Funktion?
- Welche Tests decken einen Bereich ab?
- Warum könnte ein bestimmter Ablauf fehlschlagen?
- Wo sollte eine Änderung sinnvollerweise vorgenommen werden?

Erlaubte Fähigkeiten:

- Projektbeschreibung und Skills lesen
- Dateibaum anzeigen
- Code und Konfiguration suchen
- relevante Ausschnitte lesen
- bestehende Tests finden
- mögliche Ursachen und Suchpfade erläutern

Nicht erlaubt:

- Dateien editieren
- Shell oder Tests ausführen
- Git-Branches erzeugen
- Commits oder Merge Requests erstellen
- Netzwerkzugriff anfordern

Später kann der Benutzer ausdrücklich in eine schreibende Session wechseln. Dieser Wechsel muss sichtbar sein und eine neue Capability-Zuweisung erzeugen.

---

## 13. Use-Case 4: Obsidian-Wissensvault als „Second Brain“

Der zentrale Obsidian-Vault bündelt Modulwissen, Architekturentscheidungen, Prozesse, Glossar, bekannte Fallstricke und firmenweite Zusammenhänge. Da der Vault in Git liegt, kann PatchPony dieselben Sicherheits- und Reviewmechanismen wie für Code und Konfiguration verwenden.

### 13.1 Read-only-Nutzung

Über Open WebUI oder einen Ticket-Workflow kann PatchPony:

- relevante Vault-Seiten suchen und lesen,
- Wiki-Links und Backlinks nachvollziehen,
- Wissen aus mehreren freigegebenen Modulbereichen zusammenführen,
- Antworten mit konkreten Dateipfaden und Fundstellen belegen,
- veraltete, widersprüchliche oder fehlende Dokumentation als Hinweis markieren.

### 13.2 Kontrollierte Aktualisierung

Eine Wissensänderung wird nicht direkt in den produktiven Vault geschrieben:

```text
Frage, Ticket oder expliziter Pflegeauftrag
  → relevante Vault-Seiten und Projektquellen lesen
  → Änderungsvorschlag strukturiert erstellen
  → isolierte Session und Bot-Branch anlegen
  → Markdown, Frontmatter und Links patchen
  → Pfade, Syntax, Links und Projektregeln validieren
  → Diff und betroffene Wissensbereiche anzeigen
  → Commit, Push und Merge Request
  → fachliche Review durch den zuständigen Knowledge Owner
```

### 13.3 Grenzen

- Schreibzugriff nur auf im Manifest freigegebene Vault-Pfade
- keine direkte Änderung auf `main`
- keine automatische Zusammenführung des Merge Requests
- keine Ausführung von Obsidian-Plugins, Skripten, Dataview-JavaScript oder eingebetteten Befehlen
- keine unkontrollierte Verschiebung oder Massenumbenennung von Seiten
- keine Binärdateien außerhalb einer engen Attachment-Allowlist
- keine automatische Löschung fachlich relevanter Inhalte ohne explizite Freigabe
- Konflikte, gebrochene Links oder ungültiges Frontmatter führen zu `plan_only` beziehungsweise manueller Review

### 13.4 Mehrwert im Gesamtsystem

Der Vault ist kein isoliertes Wiki neben PatchPony. Er verbessert alle anderen Use-Cases: Bugreports erhalten mehr Fachkontext, Change-Requests können Modulregeln berücksichtigen und Entwicklerfragen werden nicht nur aus Code, sondern auch aus explizitem Firmenwissen beantwortet. Umgekehrt kann aus gelösten Tickets und reviewed Changes neues Wissen kontrolliert zurück in den Vault fließen.

---

## 14. Sicherheitsmodell

### 14.1 Bedrohungen

PatchPony muss mindestens folgende Fälle berücksichtigen:

- Prompt Injection in Tickets, Code, Kommentaren, Dokumentation, Skills oder dem Wissensvault
- bösartige oder kompromittierte Repository-Inhalte
- Shell- und Argument-Injection
- Pfadtraversierung und Symlink-Angriffe
- Auslesen von Credentials oder Host-Dateien
- Datenexfiltration über Netzwerk, DNS oder Logs
- Endlosschleifen, Fork-Bombs und Ressourcenerschöpfung
- manipulierte Archive und Anhänge
- Race Conditions zwischen parallelen Sessions
- doppelte Webhooks und wiederholte Git-Aktionen
- Supply-Chain-Angriffe durch Images und Dependencies
- versehentliche Veröffentlichung vertraulichen Sourcecodes an Modellanbieter

### 14.2 Sandbox-Härtung

Mindestanforderungen:

```text
non-root user
read-only root filesystem
cap-drop ALL
no-new-privileges
seccomp profile
AppArmor profile
PID limit
memory limit
CPU limit
disk quota
execution timeout
network none by default
```

Erlaubter Schreibbereich:

```text
/workspace/session
```

Nicht mounten:

```text
/var/run/docker.sock
SSH keys
Cloud credentials
Git bot tokens
Host user profiles
~/.config
~/.aws
beliebige /var/run sockets
```

Für noch stärker untrusted Code können später gVisor, Kata Containers oder MicroVMs evaluiert werden. Ein gehärteter Container auf einer dedizierten Maschine ist das realistische Ziel für Version 1.

### 14.3 Docker-Socket und Worker-Isolation

Zugriff auf den Docker-Daemon ist praktisch eine hochprivilegierte Fähigkeit. Deshalb:

- kein Docker-Socket im Gateway
- Worker nur im internen Netzwerk
- strukturierte Worker-API statt Weitergabe freier Docker-Parameter
- feste Image-Allowlist
- feste Mount- und Netzwerkregeln
- nach Möglichkeit Rootless Docker
- keine Host-Pfade aus Agenteneingaben übernehmen

### 14.4 Netzwerk

Projekt-Sandboxes erhalten standardmäßig kein Netzwerk.

Falls einzelne Projekte für Tests Netzwerk benötigen, wird kein allgemeiner Internetzugriff aktiviert. Stattdessen werden konkrete interne Ziele, Paket-Proxies oder Testdienste über eine projektbezogene Allowlist freigegeben.

Git-Fetch, Push und Merge-Request-Erstellung erfolgen außerhalb der Projekt-Sandbox durch den Worker.

### 14.5 Secrets

- Secrets werden nie in Tickets, Modellprompts, Skill-Dateien oder Worktrees geschrieben.
- Git-Bot-Credentials bleiben beim Worker.
- n8n verwendet einen eigenen PatchPony-Service-Account.
- Secrets werden möglichst über einen vorhandenen Secret Store bereitgestellt.
- Logs und Audit-Ereignisse werden serverseitig redigiert.
- Projekt-Sandboxes bekommen nur explizit benötigte kurzlebige Test-Credentials; Version 1 sollte möglichst ganz darauf verzichten.

### 14.6 Prompt Injection

Tickettext, Repository-Inhalte, Dokumentation, Kommentare, Anhänge, Skills und Vault-Seiten gelten immer als untrusted Data.

Ein Text innerhalb des Repositories kann keine PatchPony-Policy ändern. Der Agent kann eine Aktion vorschlagen, aber nur der Gateway und Worker entscheiden anhand fest codierter Regeln und des Projektmanifests, ob sie zulässig ist.

### 14.7 Wissensvault und Obsidian-Inhalte

- `.obsidian/plugins/**`, `.obsidian/snippets/**` und andere ausführbare Erweiterungen sind immer verboten.
- YAML-Frontmatter wird mit Alias-, Größen- und Tiefenlimits geparst und optional gegen ein Schema validiert.
- Markdown-, Wiki- und Attachment-Links werden kanonisch aufgelöst; Pfadtraversierung und Links außerhalb freigegebener Vault-Bereiche werden abgelehnt.
- Eingebettetes HTML, Skriptblöcke, `data:`-URLs und ausführbare Attachments werden nicht ausgeführt und können projektbezogen vollständig verboten werden.
- Umbenennungen und Verschiebungen benötigen eine Link-Auswirkungsanalyse; Massenänderungen überschreiten in Version 1 die automatische Policy.
- Eine in einer Vault-Seite formulierte Anweisung kann keine PatchPony-Capability oder Policy verändern.

### 14.8 Anhänge

Zoho-Anhänge benötigen eine eigene Eingangskontrolle:

- maximale Dateigröße
- maximale Gesamtgröße pro Ticket
- MIME- und Magic-Byte-Prüfung
- sichere Archivextraktion
- Schutz gegen Zip-Bombs und Pfadtraversierung
- keine automatische Ausführung
- temporäre, isolierte Speicherung
- definierte Aufbewahrungsfrist

Ein Virenscanner kann ergänzend eingesetzt werden, ersetzt diese Prüfungen aber nicht.

### 14.9 Authentifizierung und Autorisierung

Der Remote-Zugriff benötigt:

- TLS
- OIDC/OAuth für Benutzer, möglichst über den vorhandenen Identity Provider
- separaten Service-Account für n8n
- kurze Token-Laufzeiten
- Projekt- und Tool-basierte Scopes
- Rate Limits
- vollständiges Audit

Reverse Proxy und Identity Provider authentifizieren den Aufrufer. PatchPony prüft zusätzlich jede fachliche Berechtigung selbst.

---

## 15. Zuverlässigkeit und Workflow-Sicherheit

### 15.1 Idempotenz

Zoho und n8n können Webhooks oder Schritte wiederholen. Jeder eingehende Event erhält einen Idempotenzschlüssel, beispielsweise aus:

```text
source + project_id + ticket_id + ticket_revision + event_type
```

Eine Wiederholung liefert den vorhandenen Jobstatus zurück, anstatt einen zweiten Branch oder Merge Request zu erzeugen.

### 15.2 Locks und Parallelität

- ein Session-Lock schützt jeden Worktree
- ein optionaler Projekt-/Pfad-Lock verhindert konkurrierende Config-Änderungen
- Git-Aktionen werden pro Branch serialisiert
- Jobs können abgebrochen werden
- abgelaufene Locks werden kontrolliert wieder freigegeben

### 15.3 Timeouts und Quotas

Limits werden pro Projekt und Aktion definiert:

- maximale Sessiondauer
- maximale Testdauer
- CPU und RAM
- maximale Prozessanzahl
- maximale Workspace-Größe
- maximale Diff-Größe
- maximale Anzahl geänderter Dateien
- maximale parallele Sessions

### 15.4 Fehlerverhalten

- kein stilles Weiterarbeiten nach fehlgeschlagenem Validator
- keine Commits nach fehlgeschlagenen Tests, sofern Policy dies nicht explizit als Draft erlaubt
- partielle Git-Aktionen werden erkannt und im Audit markiert
- ein fehlgeschlagener Push kann wiederholt werden, ohne einen neuen Commit zu erzeugen
- der Zoho-Kommentar enthält eine verständliche Zusammenfassung, keine internen Stacktraces

---

## 16. Audit und Beobachtbarkeit

Für jeden Job sollen mindestens folgende Informationen nachvollziehbar sein:

- Auslöser und Identität des Aufrufers
- Ticket, Projekt und Repository-Revision
- verwendete Skills, gelesene Projektdateien und Vault-Seiten
- aufgerufene Tools mit redigierten Parametern
- Policy-Entscheidungen
- angelegte Session und Branch
- Diff-Prüfsumme
- Validatoren und Testergebnisse
- Freigaben
- Commit und Merge Request
- Fehler und Abbruchgrund
- Start-, End- und Laufzeiten

Empfohlene technische Basis:

- strukturierte JSON-Logs über `Microsoft.Extensions.Logging`, bei Bedarf mit Serilog
- Correlation-ID vom Zoho-Webhook bis zum Merge Request
- OpenTelemetry für Traces und Metriken
- vorhandener zentraler Log-Stack oder Loki
- Prometheus/Grafana, sofern bereits im Einsatz

Quellcode, Ticketinhalte und Secrets werden nicht pauschal vollständig in Logs geschrieben.

---

## 17. Datenschutz und Modellzugriff

Vor der produktiven Nutzung muss geklärt werden:

- welcher Modellanbieter erhält Quellcode, Ticketdaten und Inhalte aus dem Wissensvault?
- werden Eingaben oder Ausgaben beim Anbieter gespeichert?
- werden sie für Training verwendet?
- in welcher Region werden Daten verarbeitet?
- welche Projekte oder Verzeichnisse dürfen den jeweiligen Anbieter erreichen?
- welche personenbezogenen Daten können in Zoho-Tickets enthalten sein?
- wie lange werden Prompts, Diffs und Anhänge aufbewahrt?

PatchPony sollte projektbezogen festlegen können, welche Modelle beziehungsweise Provider erlaubt sind. Besonders sensible Projekte können auf ein intern betriebenes Modell beschränkt werden.

---

## 18. Betrieb und Supply Chain

Empfohlene Werkzeuge und Regeln:

- Docker Compose für Version 1; Kubernetes ist zunächst nicht erforderlich
- Images und Actions versionsgenau pinnen
- Trivy oder vergleichbarer Scanner
- Renovate oder Dependabot für Updates
- SBOM-Erzeugung im Build
- signierte Images optional als spätere Härtung
- getrennte Development-, Staging- und Production-Konfiguration
- regelmäßige Wiederherstellungstests für PostgreSQL-Backups
- definierte Rotation der Bot- und Service-Account-Credentials

Redis, RabbitMQ oder NATS sind für den ersten Einzel-Worker nicht zwingend. Eine Job-Queue und Locks können zunächst mit PostgreSQL umgesetzt werden. Bei mehreren Workern oder hohem Durchsatz kann später ein dediziertes Queue-System ergänzt werden.

---

## 19. Teststrategie

### 19.1 Technische Tests

- Unit-Tests für Policy-, Pfad- und Parameterprüfung
- Integrationstests für MCP und REST
- MCP-Conformance-Tests
- Docker-/Sandbox-Integrationstests
- Git-Worktree- und Branch-Tests
- Markdown-, Frontmatter-, Wiki-Link- und Backlink-Tests für den Wissensvault
- Tests für Abbruch, Timeout und Cleanup
- Tests für doppelte Webhooks
- Tests für konkurrierende Sessions

### 19.2 Security-Tests

- Pfadtraversierung
- Symlink-Ausbruch
- Shell- und Argument-Injection
- verbotene Mounts
- Netzwerk-Exfiltrationsversuche
- Zugriff auf Host- und Credential-Pfade
- Fork-Bombs und Ressourcenerschöpfung
- manipulierte Archive
- Prompt-Injection-Testfälle
- Pfadtraversierung über Markdown-, Wiki- und Attachment-Links
- manipulierte Frontmatter-Aliase und unerlaubte Obsidian-Plugins

### 19.3 Agenten-Evaluation

Eine Sammlung echter, abgeschlossener Tickets dient als Regressionstest:

- erkennt die Triage fehlende Angaben?
- werden die richtigen Dateien gefunden?
- ist der Plan fachlich brauchbar?
- entstehen unnötige oder gefährliche Änderungen?
- werden Risiken korrekt eskaliert?
- bleiben Antworten über Modell- und Prompt-Updates stabil genug?
- werden passende Vault-Seiten gefunden und Quellen korrekt angegeben?
- erzeugen Wissensänderungen kleine, fachlich nachvollziehbare Diffs ohne Linkbruch?

Für den Start sind 20 bis 50 repräsentative historische Tickets sinnvoller als ausschließlich künstliche Beispiele.

---

## 20. Empfohlener V1-Umfang

Version 1 sollte bewusst klein bleiben.

### Phase 1: Read-only Fundament

- Debian-Host und Docker Compose
- .NET-Solution und C#-Projektstruktur
- Gateway, Worker und PostgreSQL
- Projektregistrierung und Manifestvalidierung
- Git-Clone und Base-Checkout
- Authentifizierung und Audit
- `project.describe`
- `project.tree`
- `skills.list` und `skills.read`
- `source.search` und `source.read`
- `knowledge.tree`, `knowledge.search`, `knowledge.read` und `knowledge.links`
- Open-WebUI-Integration

Ergebnis: Entwicklerfragen, Ticketanalyse und Fragen zum Modul- oder Firmenwissen sind read-only möglich.

### Phase 2: Sessions und Config-Änderungen

- Session- und Worktree-Lifecycle
- Pfad-Policies
- `config.read`, `config.patch`, `config.validate`
- `changes.diff` und `changes.discard`
- Ressourcenlimits und Cleanup
- n8n-/Zoho-Triage-Workflow

Ergebnis: Config-Änderungen können sicher vorbereitet und als Diff geprüft werden.

### Phase 3: Tests und Merge Requests

- registrierte Testbefehle
- gehärtete Projekt-Sandboxes
- Commit, Push und Merge-Request-Erstellung
- kontrollierte Markdown- und Frontmatter-Änderungen im Wissensvault
- Link-, Pfad- und Knowledge-Owner-Validierung
- Freigabe-Workflow
- Benachrichtigung des Dev-Teams

Ergebnis: zulässige kleine Config- und Wissensänderungen werden vollständig bis zum Review vorbereitbar.

### Phase 4: Erweiterte Implementation

- projektweise Freigabe für Source-Code-Änderungen
- Language-Server-Integration
- feinere Netzwerk-Allowlisting
- mehrere Worker
- optional gVisor, Kata oder MicroVMs
- erweiterte automatische Review- und Risikoanalyse

---

## 21. Entscheidungen für den Start

Folgende Entscheidungen können bereits als Arbeitsannahme gelten:

1. Produktname und Verzeichnisse heißen durchgehend `PatchPony` und `.patchpony/`.
2. Debian 13 ist das bevorzugte Host-System.
3. Gateway, Core und Worker werden in C# auf .NET 10 LTS implementiert.
4. PatchPony unterstützt MCP und eine REST-API.
5. n8n orchestriert; PatchPony erzwingt Policies.
6. Gateway und privilegierter Worker sind getrennt.
7. Jeder schreibende Auftrag erhält eine eigene Session und einen Git-Worktree.
8. Projekt-Sandboxes haben standardmäßig kein Netzwerk.
9. Agenten erhalten semantische Tools und keine freie Shell.
10. Änderungen werden höchstens bis zu einem Merge Request automatisiert.
11. Automatische Merges und Pushes auf geschützte Branches bleiben deaktiviert.
12. PostgreSQL speichert Jobs, Locks, Freigaben und Auditdaten.
13. Skills liefern Kontext, aber niemals Berechtigungen.
14. Externe Inhalte und Repository-Dateien gelten als untrusted.
15. Der Obsidian-Vault ist eine Git-versionierte Wissensquelle; Lesen und Schreiben bleiben pfad-, rollen- und revisionsgebunden.
16. Vault-Änderungen werden ausschließlich über isolierte Sessions und menschlich reviewte Merge Requests veröffentlicht.
17. Obsidian-Plugins, Skripte und ausführbare Inhalte werden nicht ausgeführt.

---

## 22. Noch offene Fragen

Diese Punkte müssen vor beziehungsweise während des Prototyps entschieden werden:

- GitHub, GitLab oder beide im ersten Release?
- Welche Zoho-Ticketfelder sind pro Projekt verpflichtend?
- Welcher Identity Provider ist bereits vorhanden?
- Welcher Modellprovider darf welchen Sourcecode verarbeiten?
- Läuft n8n auf derselben Maschine oder extern?
- Reicht Docker Rootless für alle benötigten Projekt-Runner?
- Welche Programmiersprachen und Testumgebungen müssen die ersten Projekte unterstützen?
- Wie werden Reviewer für Merge Requests ausgewählt?
- Welche Config-Änderungen gelten konkret als automatisch vorbereitbar?
- Wo sollen Audit-Logs und größere Testartefakte aufbewahrt werden?
- Welche Aufbewahrungsfristen gelten für Sessions, Tickets und Anhänge?
- Benötigt ein Projekt Testnetzwerke oder externe Paketquellen?
- Liegt der Wissensvault in einem eigenen Repository und welche Bereiche werden welchem PatchPony-Projekt zugeordnet?
- Welche Frontmatter-Felder, Linkregeln und Attachment-Typen sind verbindlich?
- Wer ist fachlicher Knowledge Owner und Reviewer je Vault-Bereich?
- Welche Vault-Bereiche dürfen vom jeweiligen Modellprovider verarbeitet werden?

Diese Fragen blockieren den Read-only-Prototyp nicht. Sie sollten jedoch beantwortet sein, bevor PatchPony Schreib- und Git-Rechte erhält.

---

## 23. Referenzen

- [Model Context Protocol – C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [.NET Support Policy](https://dotnet.microsoft.com/en-us/platform/support/policy)
- [.NET Container Images](https://learn.microsoft.com/dotnet/core/docker/container-images)
- [Model Context Protocol – SDK Tiering](https://modelcontextprotocol.io/community/sdk-tiers)
- [Debian Releases](https://www.debian.org/releases/)
- [Docker Engine auf Debian](https://docs.docker.com/engine/install/debian/)
- [Docker Engine Security](https://docs.docker.com/engine/security/)
- [Docker Rootless Mode](https://docs.docker.com/engine/security/rootless/)
