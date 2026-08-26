# PatchPony – Projekt- und Iterationsplan

Status: Planung  
Aktuelle Iteration: I0 – Entscheidungen und Projektvorbereitung  
Technisches Ziel: sichere, projektbezogene Agenten-Runtime mit MCP- und REST-Schnittstelle  
Begleitdokument: [Architektur- und Umsetzungskonzept](brainstorm/patchpony-concept.md)

## 1. Zweck dieses Plans

Dieser Plan übersetzt das Architekturkonzept in eine umsetzbare Reihenfolge. Er beschreibt nicht nur, was gebaut wird, sondern auch, wann eine Fähigkeit freigeschaltet werden darf und welche Tests vorher erfolgreich sein müssen.

Die Iterationen sind bewusst ergebnisorientiert. Ihre Dauer hängt davon ab, wie viel Zeit neben dem Tagesgeschäft verfügbar ist. Eine Iteration wird abgeschlossen, bevor die nächste sicherheitsrelevante Fähigkeit freigeschaltet wird.

Der Plan wird während der Implementierung fortgeschrieben:

- Der Status einer Iteration wird auf `geplant`, `aktiv`, `blockiert` oder `abgeschlossen` gesetzt.
- Neue Erkenntnisse werden zuerst als Entscheidung oder Backlog-Eintrag dokumentiert.
- Architekturänderungen erhalten ein Architecture Decision Record, kurz ADR.
- Eine Iteration gilt erst nach Erfüllung ihrer Abnahmekriterien als abgeschlossen.
- Bewusst verschobene Arbeit wird in den späteren Backlog aufgenommen und nicht stillschweigend vergessen.

---

## 2. Festgelegter Stack

| Bereich | Entscheidung |
|---|---|
| Produktname | PatchPony |
| Anwendung | C# auf .NET 10 LTS |
| HTTP | ASP.NET Core Minimal APIs |
| MCP | `ModelContextProtocol.AspNetCore` mit Streamable HTTP |
| Worker | .NET `BackgroundService` |
| Datenbank | PostgreSQL mit Npgsql und Entity Framework Core |
| Migrationen | Entity Framework Core Migrations |
| Tests | xUnit, Testcontainers for .NET, MCP-Conformance-Tests |
| Host | Debian 13 minimal |
| Container | Docker Engine und Docker Compose |
| Workflow | n8n |
| Interaktive Nutzung | Open WebUI über eine Pipe zu n8n |
| Git | nativer Git-CLI über sichere `ProcessStartInfo.ArgumentList`-Aufrufe |
| Wissensbasis | Git-versionierter Obsidian-Vault aus Markdown-Dateien |
| Observability | `Microsoft.Extensions.Logging` und OpenTelemetry |
| Ergänzende Sprache | Python für Evaluationen und isolierte Hilfstools |

---

## 3. Lieferstufen

Die Entwicklung wird durch vier Fähigkeitsgrenzen strukturiert:

```text
Stufe A: Read-only
    Projekt beschreiben, Skills, Code, Config und Wissensvault lesen
        │
        ▼
Stufe B: Isoliert ändern
    Session und Worktree anlegen, Config oder Vault-Markdown patchen, Diff erzeugen
        │
        ▼
Stufe C: Kontrolliert ausführen
    registrierte Validatoren, Linkprüfungen und Tests in gehärteter Sandbox starten
        │
        ▼
Stufe D: Zur Review veröffentlichen
    Branch, Commit, Push und Merge Request über Git-Bot erzeugen
```

Eine spätere Stufe wird technisch nicht aktiviert, bevor die Sicherheits- und Abnahmekriterien der vorherigen Stufe erfüllt sind.

### 3.1 Geplante Releases

| Release | Iterationen | Ergebnis |
|---|---|---|
| Read-only MVP | I0–I6 | Entwickler können über Open WebUI sicher Fragen zu Code und Wissensvault stellen. |
| Change Proposal MVP | I7–I9 | Config-Änderungen können isoliert vorbereitet, validiert und als Diff ausgegeben werden. |
| PatchPony V1 | I10–I13 | Tickets, Config- und Wissensänderungen können bis zu einem Merge Request vorbereitet werden. |
| Production Pilot | I14 | Betrieb, Security und Recovery sind für ein Pilotprojekt abgesichert. |

---

## 4. Iterationsübersicht

| ID | Iteration | Größe | Abhängigkeit | Status |
|---|---|---:|---|---|
| I0 | Entscheidungen und Projektvorbereitung | S | – | geplant |
| I1 | Solution, Build und lokale Infrastruktur | M | I0 | geplant |
| I2 | Domänenmodell, Persistenz und Job-Lifecycle | M | I1 | geplant |
| I3 | Projektkatalog und sichere Read-only-Runtime | L | I2 | geplant |
| I4 | MCP- und REST-Read-only-Schnittstellen | M | I3 | geplant |
| I5 | Authentifizierung, Policies und Audit | L | I4 | geplant |
| I6 | Open WebUI, n8n und Entwicklerfragen | M | I5 | geplant |
| I7 | Sessions, Branches und Git-Worktrees | L | I6 | geplant |
| I8 | Config-Patches und Validierung | L | I7 | geplant |
| I9 | Gehärtete Sandbox und registrierte Tests | L | I8 | geplant |
| I10 | Commit, Push und Merge Request | L | I9 | geplant |
| I11 | Zoho-Task-Workflow | L | I10 | geplant |
| I12 | Feature- und Change-Request-Workflow | M | I11 | geplant |
| I13 | Obsidian-Wissensvault lesen und pflegen | L | I12 | geplant |
| I14 | Produktionshärtung und Pilotbetrieb | L | I13 | geplant |

Größen sind relativ:

- `S`: wenige klar abgegrenzte Arbeitspakete
- `M`: mehrere Komponenten oder Integrationen
- `L`: sicherheitsrelevante oder systemübergreifende Arbeit mit umfangreichen Tests

---

## 5. Allgemeine Definition of Done

Ein Arbeitspaket gilt nur als erledigt, wenn alle zutreffenden Punkte erfüllt sind:

- Verhalten ist implementiert und lokal reproduzierbar.
- Positive und negative Tests sind vorhanden.
- Formatierung, Build und statische Analyse sind erfolgreich.
- Nullable Reference Types bleiben aktiviert.
- Compiler-Warnungen werden nicht pauschal unterdrückt.
- Öffentliche Eingaben besitzen Größen- und Formatgrenzen.
- Dateipfade, Toolnamen und Prozessargumente werden serverseitig geprüft.
- Timeouts und Abbruch über `CancellationToken` sind umgesetzt.
- Logs enthalten eine Correlation-ID und keine Secrets.
- Sicherheitsrelevante Entscheidungen werden auditiert.
- Die Dokumentation beschreibt Konfiguration und Betrieb.
- Docker-Images laufen als non-root, soweit technisch möglich.
- Keine neue Netzwerk-, Schreib- oder Ausführungsfähigkeit wird implizit freigeschaltet.
- Änderungen am Bedrohungsmodell wurden geprüft und dokumentiert.

Für jede Iteration kommen spezifische Abnahmekriterien hinzu.

---

## 6. I0 – Entscheidungen und Projektvorbereitung

Status: geplant  
Ziel: Alle Entscheidungen treffen, die den Projektzuschnitt der ersten sechs Iterationen beeinflussen.

### 6.1 Arbeitspakete

- [ ] `I0.1` Erstes Pilotprojekt auswählen.
- [ ] `I0.2` Ersten Git-Provider auswählen: GitHub oder GitLab, nicht beide gleichzeitig.
- [ ] `I0.3` Repository und gewünschte Branch-Strategie für PatchPony festlegen.
- [ ] `I0.4` Entscheiden, welcher Identity Provider für Benutzer vorgesehen ist.
- [ ] `I0.5` Service-Account-Konzept für n8n festlegen.
- [ ] `I0.6` Modellprovider und Datenschutzgrenzen für das Pilotprojekt festlegen.
- [ ] `I0.7` Deployment-Topologie von n8n und Open WebUI dokumentieren.
- [ ] `I0.8` Erste unterstützte Projektsprachen und Testumgebungen auswählen.
- [ ] `I0.9` Aufbewahrungsfristen für Jobs, Sessions, Logs und Anhänge festlegen.
- [x] `I0.10` Versioniertes Threat Model mit Trust Boundaries, Bedrohungen, Controls und Annahmen erstellt.
- [ ] `I0.11` ADR-Verzeichnis und ADR-Template definieren.
- [ ] `I0.12` Product-Backlog und Issue-Labels anlegen.
- [ ] `I0.13` Repository, Pfadmodell und fachliche Eigentümer des Obsidian-Wissensvaults festlegen.
- [ ] `I0.14` Lesbare und schreibbare Vault-Bereiche sowie erlaubte Attachment-Typen definieren.

### 6.2 Benötigte Entscheidungen

| Entscheidung | Default, falls keine Präferenz besteht |
|---|---|
| Git-Provider | der Provider des Pilotprojekts |
| Pilotumfang | ein Repository, ein Projekt, eine Benutzergruppe |
| Sourcecode-Schreiben | in V1 deaktiviert |
| Config-Schreiben | erst ab I8 und nur für erlaubte Pfade |
| Wissensvault lesen | im Read-only-MVP für freigegebene Pfade |
| Wissensvault schreiben | erst ab I13, isoliert und ausschließlich über Merge Request |
| Automatisches Merge | deaktiviert |
| Sandbox-Netzwerk | standardmäßig `none` |
| Open-WebUI-Zugriff | nur read-only bis I7 abgeschlossen ist |

### 6.3 Ergebnisse

- dokumentierte Entscheidungen statt impliziter Annahmen
- ausgewähltes Pilotprojekt
- priorisierter Backlog
- initiales Threat Model
- klare Grenze für Daten, die an ein Modell gesendet werden dürfen
- definierte Knowledge Owner und Reviewregeln für den Wissensvault

### 6.4 Abnahmekriterien

- Alle Entscheidungen, die I1–I3 beeinflussen, sind beantwortet.
- Der Pilot hat einen fachlichen Ansprechpartner und einen technischen Reviewer.
- Für Git-, Modell- und Identity-Provider existiert jeweils eine Entscheidung.
- Repository, Pfadfreigaben und Knowledge Owner des Wissensvaults sind festgelegt.
- Offene Punkte haben Eigentümer oder bleiben nachweislich ohne Einfluss auf den Read-only-MVP.

---

## 7. I1 – Solution, Build und lokale Infrastruktur

Status: geplant  
Ziel: Ein reproduzierbares, leeres PatchPony-System lässt sich lokal bauen, testen und mit PostgreSQL starten.

### 7.1 Arbeitspakete

- [ ] `I1.1` Git-Repository initialisieren und Remote konfigurieren.
- [ ] `I1.2` `PatchPony.sln` und die Projekte `Core`, `Contracts`, `Infrastructure`, `Gateway`, `Worker` und `Cli` anlegen.
- [ ] `I1.3` Testprojekte für Unit-, Integration-, Policy- und Security-Tests anlegen.
- [ ] `I1.4` `Directory.Build.props`, `.editorconfig` und Central Package Management konfigurieren.
- [ ] `I1.5` Nullable Reference Types, deterministische Builds und Analyzer aktivieren.
- [ ] `I1.6` ASP.NET-Core-Gateway mit Health-Endpunkten erstellen.
- [ ] `I1.7` Worker als .NET `BackgroundService` erstellen.
- [ ] `I1.8` Dockerfiles für Gateway und Worker als Multi-Stage-Build erstellen.
- [ ] `I1.9` Docker Compose mit Gateway, Worker und PostgreSQL erstellen.
- [ ] `I1.10` lokale Konfiguration ohne eingecheckte Secrets umsetzen.
- [ ] `I1.11` CI für Restore, Build, Test und Container-Build einrichten.
- [ ] `I1.12` grundlegendes Dependency- und Image-Scanning aktivieren.

### 7.2 Technische Leitplanken

- Gateway und Worker sind separate Prozesse und Images.
- Der Gateway besitzt keinen Docker-Socket.
- Der Worker ist nicht über einen veröffentlichten Host-Port erreichbar.
- Production-Images enthalten kein .NET SDK.
- Paketversionen werden zentral und reproduzierbar verwaltet.
- Konfiguration folgt `appsettings.json`, Environment-Variablen und Secret Provider; Secrets bleiben außerhalb des Repositories.

### 7.3 Tests

- Solution baut auf Entwicklerrechner und CI.
- Unit-Test-Skeleton läuft.
- Testcontainers kann PostgreSQL starten.
- Compose-Stack meldet Gateway, Worker und Datenbank als gesund.
- Gateway kann den Worker nicht über einen öffentlichen Port erreichen.
- Container laufen mit dem vorgesehenen non-root-Benutzer.

### 7.4 Demo

```text
docker compose up
GET /health/live  → 200
GET /health/ready → 200
dotnet test       → erfolgreich
```

### 7.5 Abnahmekriterien

- Ein neuer Checkout lässt sich anhand der README ohne manuelle Sonderkonfiguration starten.
- CI und lokale Tests sind grün.
- Es existiert noch keine Projekt-, Schreib-, Shell- oder Git-Fähigkeit.

---

## 8. I2 – Domänenmodell, Persistenz und Job-Lifecycle

Status: geplant  
Ziel: PatchPony besitzt einen transportunabhängigen Core und kann Projekte, Jobs, Sessions und Audit-Ereignisse konsistent speichern.

### 8.1 Domänenobjekte

- `Project`
- `RepositoryRegistration`
- `Job`
- `JobStep`
- `Session`
- `Approval`
- `AuditEvent`
- `ArtifactReference`
- `ToolInvocation`
- `IdempotencyRecord`

### 8.2 Arbeitspakete

- [x] `I2.1` Aggregate und Value Objects in `PatchPony.Core` modellieren.
- [x] `I2.2` Job-Zustandsmaschine mit erlaubten Übergängen implementieren.
- [x] `I2.3` stabile Fehlercodes und Ergebnisobjekte definieren.
- [x] `I2.4` PostgreSQL-DbContext und erste Migration erstellen.
- [x] `I2.5` Repositories beziehungsweise Application Services definieren.
- [x] `I2.6` Idempotenzservice implementieren.
- [x] `I2.7` Datenbankbasierte Job-Queue für einen Worker implementieren.
- [x] `I2.8` Job-Claiming mit Lock und Ablaufzeit umsetzen.
- [x] `I2.9` Audit-Schnittstelle und append-only Audit-Ereignisse implementieren.
- [x] `I2.10` Correlation-ID durch Gateway, Core und Worker führen.
- [x] `I2.11` Retention-Schnittstellen ohne automatische Löschung vorbereiten.

### 8.3 Tests

- jeder erlaubte Statusübergang
- jeder verbotene Statusübergang
- doppelter Idempotenzschlüssel erzeugt keinen zweiten Job
- zwei Worker können denselben Job nicht gleichzeitig claimen
- abgelaufener Claim kann kontrolliert übernommen werden
- Audit-Ereignisse sind nachträglich nicht über normale Application Services änderbar
- Datenbankmigration funktioniert auf leerer und bereits initialisierter Datenbank

### 8.4 Abnahmekriterien

- Core kennt weder ASP.NET Core noch MCP-Typen.
- Ein Job kann angelegt, beansprucht, abgeschlossen, abgebrochen und als fehlgeschlagen markiert werden.
- Jeder Zustandswechsel erzeugt ein Audit-Ereignis.
- Noch kein Prozess kann Quellcode oder Repositories lesen.

---

## 9. I3 – Projektkatalog und sichere Read-only-Runtime

Status: aktiv
Ziel: Ein registriertes Pilotprojekt und die freigegebenen Bereiche des Wissensvaults können lokal und read-only beschrieben, durchsucht und gelesen werden.

### 9.1 Arbeitspakete

- [x] `I3.1` Format und JSON Schema für `.patchpony/project.yaml` finalisieren.
- [x] `I3.2` strikte Manifest-Deserialisierung und Schema-Validierung implementieren.
- [x] `I3.3` Projektregistrierung mit Repository-URL und Default-Branch implementieren.
- [x] `I3.4` kontrollierten Base-Checkout beziehungsweise Fetch implementieren.
- [x] `I3.5` Repository-Revisionen unveränderlich referenzieren.
- [x] `I3.6` kanonische Pfadauflösung implementieren.
- [x] `I3.7` Schutz gegen `..`, absolute Pfade, alternative Separatoren und Symlink-Ausbruch implementieren.
- [x] `I3.8` Readable-, Writable- und Forbidden-Pfade auswerten.
- [x] `I3.9` Skill-Katalog und Skill-Reader implementieren.
- [x] `I3.10` Projektbaum mit Depth-, Count- und Größenlimits implementieren.
- [x] `I3.11` Source-Suche mit festen Argumenten und Ergebnislimits implementieren.
- [x] `I3.12` Source-Reader mit Byte-, Zeilen- und Encoding-Limits implementieren.
- [x] `I3.13` CLI-Kommandos für Projektvalidierung und Read-only-Diagnose erstellen.
- [x] `I3.14` Git-versionierten Wissensvault als eigene, revisionsgebundene Knowledge Source registrieren.
- [x] `I3.15` Vault-Baum, Markdown-Reader und textuelle Suche mit Pfad-, Treffer- und Größenlimits implementieren.
- [x] `I3.16` Wiki-Links, Markdown-Links und Backlinks ohne Ausführung von Obsidian-Plugins auflösen.

### 9.2 Interne Capabilities

```text
project.describe
project.tree
skills.list
skills.read
source.search
source.read
config.list
config.read
knowledge.tree
knowledge.search
knowledge.read
knowledge.links
```

Diese Fähigkeiten werden in dieser Iteration intern beziehungsweise über die CLI getestet. Die öffentliche MCP-Freigabe folgt in I4 und die externe Freigabe erst nach I5.

### 9.3 Sicherheitsanforderungen

- Der Base-Checkout ist für Agentenoperationen read-only.
- Git-Credentials sind nicht Teil des Workspaces.
- Binärdateien werden nicht ungeprüft als Text ausgegeben.
- Suchergebnisse besitzen Datei-, Treffer- und Bytegrenzen.
- Verbotene Pfade bleiben auch dann gesperrt, wenn ein Symlink darauf zeigt.
- Fehler geben keine Host-Pfade außerhalb des Projektkontexts preis.
- `.obsidian/plugins/**`, Skripte und nicht freigegebene Attachments werden nicht geladen oder ausgeführt.
- Vault-Links werden kanonisch aufgelöst und können freigegebene Pfade nicht verlassen.

### 9.4 Tests

- gültiges und ungültiges Projektmanifest
- unbekannte Manifestfelder
- Pfadtraversierung mit Windows- und Linux-Separatoren
- Symlinks innerhalb und außerhalb des Projekts
- sehr große Dateien
- Binärdateien und ungültiges Encoding
- extrem viele Suchtreffer
- verbotene und nicht freigegebene Pfade
- Repository ohne `.patchpony`-Verzeichnis
- wechselnde Branch-Spitze bei fest referenzierter Revision
- Pfadtraversierung über Wiki-, Markdown- und Attachment-Links
- sehr großes oder rekursives Linknetz
- ausführbare Obsidian-Plugins und unerlaubte Attachments

### 9.5 Demo

```text
patchpony project validate <pilot>
patchpony project describe <pilot>
patchpony source search <pilot> "RefreshToken"
patchpony source read <pilot> src/Auth/TokenService.cs
patchpony knowledge search <pilot> "Buchungslogik"
patchpony knowledge read <pilot> modules/billing/overview.md
```

### 9.6 Abnahmekriterien

- Das Pilotprojekt ist reproduzierbar registriert.
- Alle Read-only-Operationen arbeiten ausschließlich innerhalb des erlaubten Projektbereichs.
- Security-Tests für Pfade und Symlinks sind grün.
- Eine Modulfrage kann revisionsgebunden aus Code und freigegebenem Vault-Wissen beantwortet werden.
- Es existiert weiterhin keine schreibende oder ausführende Agentenfähigkeit.

---

## 10. I4 – MCP- und REST-Read-only-Schnittstellen

Status: geplant  
Ziel: Die internen Read-only-Fähigkeiten stehen über wohldefinierte MCP-Tools und REST-Endpunkte zur Verfügung.

### 10.1 Arbeitspakete

- [x] `I4.1` offizielles MCP C# SDK integrieren.
- [x] `I4.2` Streamable-HTTP-Endpunkt `/mcp` konfigurieren.
- [x] `I4.3` REST-Versionierung unter `/api/v1` einrichten.
- [x] `I4.4` MCP-Tools auf Core-Anwendungsfälle abbilden.
- [x] `I4.5` äquivalente REST-Endpunkte für n8n bereitstellen.
- [x] `I4.6` einheitliche Fehlercodes für MCP und REST abbilden.
- [x] `I4.7` Request-, Body-, Ergebnis- und Parallelitätslimits implementieren.
- [x] `I4.8` Cancellation und Timeouts bis in Datei- und Suchoperationen weiterreichen.
- [x] `I4.9` Toolschemas als stabile Verträge testen.
- [x] `I4.10` MCP-Inspector- und Conformance-Tests einrichten.
- [x] `I4.11` OpenAPI-Dokument für die REST-API erzeugen und aktuell halten.
  - Swagger UI ausschließlich im lokalen Entwicklungsmodus bereitstellen.
  - In Produktion OpenAPI-JSON nur intern oder nach Authentifizierung verfügbar machen.
  - Authentifizierungsschema, Fehlerformate und repräsentative Beispielrequests dokumentieren.

### 10.2 REST-Schnittstellen der ersten Version

```text
GET  /api/v1/projects
GET  /api/v1/projects/{projectId}
GET  /api/v1/projects/{projectId}/tree
GET  /api/v1/projects/{projectId}/skills
GET  /api/v1/projects/{projectId}/skills/{skillId}
POST /api/v1/projects/{projectId}/search
GET  /api/v1/projects/{projectId}/files/{path}
POST /api/v1/projects/{projectId}/knowledge/search
GET  /api/v1/projects/{projectId}/knowledge/files/{path}
GET  /api/v1/projects/{projectId}/knowledge/links/{path}
```

REST und MCP verwenden dieselben Core-Handler. Business- und Sicherheitslogik wird nicht in Controllern oder MCP-Toolklassen dupliziert.

### 10.3 Tests

- MCP-Conformance für die unterstützte Protokollversion
- Kompatibilität mit der geplanten Open-WebUI-Version
- REST-Contract-Tests
- Schema-Snapshots für alle MCP-Tools
- große und ungültige Requests
- Cancellation während einer Suche
- parallel aufgerufene Read-only-Tools
- identische Policy-Ergebnisse über REST und MCP

### 10.4 Abnahmekriterien

- MCP Inspector kann Tools auflisten und aufrufen.
- REST und MCP liefern fachlich äquivalente Ergebnisse.
- Die Schnittstellen sind nur im lokalen beziehungsweise internen Entwicklungsnetz erreichbar.
- Externe Freigabe erfolgt erst nach I5.

---

## 11. I5 – Authentifizierung, Policies und Audit

Status: geplant  
Ziel: Jeder externe Aufruf ist authentifiziert, projekt- und toolbezogen autorisiert und vollständig auditierbar.

### 11.1 Arbeitspakete

- [x] `I5.1` OIDC/JWT-Authentifizierung für Benutzer integrieren.
- [x] `I5.2` Service-Account-Authentifizierung für n8n integrieren.
- [x] `I5.3` Rollen und Scopes aus dem Architekturkonzept implementieren.
- [x] `I5.4` Autorisierung auf Projekt, Tool und Parameter anwenden.
- [x] `I5.5` Default-Deny-Policy durchsetzen.
- [x] `I5.6` Worker-Aufträge intern authentifizieren und signieren beziehungsweise eindeutig zuordnen.
- [x] `I5.7` Audit für Authentifizierungs-, Policy- und Toolentscheidungen ergänzen.
- [x] `I5.8` TLS über Reverse Proxy konfigurieren.
- [x] `I5.9` Rate Limits und Schutz gegen Request-Flooding ergänzen.
- [x] `I5.10` CORS standardmäßig deaktivieren oder eng begrenzen.
- [x] `I5.11` Log-Redaction für Tokens, Ticketdaten und mögliche Secrets implementieren.
- [x] `I5.12` negative Policy-Testmatrix erstellen.

### 11.2 Erste Rollen

```text
code-reader
issue-planner
knowledge-reader
knowledge-editor
config-editor
reviewer
service-n8n
```

In I5 erhalten alle Rollen ausschließlich Read-only-Capabilities. Schreibrechte werden erst gemeinsam mit den jeweiligen Werkzeugen in späteren Iterationen aktiviert; für `knowledge-editor` geschieht dies frühestens in I13.

### 11.3 Tests

- fehlendes, abgelaufenes und falsch signiertes Token
- gültiger Benutzer ohne Projektrecht
- gültiger Benutzer mit falschem Tool-Scope
- n8n-Service-Account auf nicht freigegebenem Projekt
- manipulierte Projekt- oder Tool-ID
- Rate-Limit-Verhalten
- Logs und Audit ohne Klartext-Token
- Gateway-Container ohne Docker-Socket, Git-Token und Projekt-Write-Mount

### 11.4 Security Gate A

Read-only-MCP darf erst extern erreichbar werden, wenn:

- Default-Deny nachgewiesen ist,
- alle negativen Policy-Tests grün sind,
- TLS aktiv ist,
- Audit und Correlation-ID funktionieren,
- der Gateway keine privilegierten Mounts oder Credentials besitzt.

### 11.5 Abnahmekriterien

- Jeder externe Request besitzt eine nachvollziehbare Identität.
- Jede Toolausführung erzeugt ein Policy- und Audit-Ereignis.
- Nicht autorisierte Zugriffe liefern keine Projektdetails.
- Security Gate A ist dokumentiert bestanden.

---

## 12. I6 – Open WebUI, n8n und Entwicklerfragen

Status: geplant  
Ziel: Use-Case 3 und der lesende Teil von Use-Case 4 funktionieren als erster vollständiger, aber read-only Vertical Slice.

### 12.1 Arbeitspakete

- [x] `I6.1` Open-WebUI-Pipe zu n8n konfigurieren beziehungsweise implementieren.
- [x] `I6.2` n8n-Credentials als eigenen PatchPony-Service-Account einrichten.
- [x] `I6.3` Read-only-Agentenworkflow in n8n erstellen.
- [x] `I6.4` Projektwahl und Benutzeridentität kontrolliert weiterreichen.
- [x] `I6.5` Antworten mit Dateipfad und relevanter Fundstelle versehen.
- [x] `I6.6` Toolaufrufe und Quellen für den Benutzer nachvollziehbar darstellen.
- [x] `I6.7` Limits für maximale Agentenschritte und Toolaufrufe setzen.
- [x] `I6.8` Timeout-, Abbruch- und Fehlermeldungen benutzerfreundlich gestalten.
- [x] `I6.9` 10 bis 20 repräsentative Entwicklerfragen als Evaluation erfassen.
- [x] `I6.10` Datenschutzprüfung des tatsächlich übertragenen Kontexts durchführen.
- [x] `I6.11` Fragen zu Modul- und Firmenwissen mit Vault-Quellen in die Evaluation aufnehmen.

### 12.1a Pilotentscheidungen

Die verbindlichen Annahmen für Hosting, Identitäten, Modelle, Datenhaltung und
Sicherheitsgrenzen des ersten Piloten stehen in
[docs/i6-pilot-decisions.md](docs/i6-pilot-decisions.md). Insbesondere läuft
Open WebUI unter `oi.destination.one`, n8n unter `n8n.oi.destination.one`, und
`gpt-oss:20b` ist das erste Testmodell. Die OIDC-Integration über
`id.destination.one` bleibt für die produktive Ausbaustufe vorgesehen.
### 12.2 Unterstützte Interaktionen

- Code und Konfiguration finden
- Modulverantwortung erklären
- relevante Skills und Dokumentation nennen
- passende Vault-Seiten, Links und Wissenslücken nennen
- bestehende Tests finden
- mögliche Ursachen und weitere Suchschritte vorschlagen

Nicht unterstützt:

- Dateiänderungen
- Tests oder Shell ausführen
- Branches oder Commits erstellen
- allgemeiner Netzwerkzugriff

### 12.3 Tests und Evaluation

- Benutzer sieht nur erlaubte Projekte.
- Projektwechsel erzeugt keine Kontextvermischung.
- Antwort verweist auf existierende, erlaubte Dateien.
- Prompt Injection in Code oder Skill kann keine zusätzliche Capability aktivieren.
- Prompt Injection in einer Vault-Seite kann weder Capability noch Policy verändern.
- maximale Agentenschritte werden eingehalten.
- Abbruch beendet laufende Suche und n8n-Workflow.
- Antwortqualität wird gegen die vorbereiteten Entwicklerfragen bewertet.

### 12.4 Release: Read-only MVP

Nach I6 kann PatchPony für eine kleine Pilotgruppe als read-only Werkzeug für Code- und Wissensfragen verwendet werden.

### 12.5 Abnahmekriterien

- Eine reale Entwicklerfrage durchläuft Open WebUI, n8n und PatchPony Ende-zu-Ende.
- Eine reale Wissensfrage wird aus einem freigegebenen Vault-Bereich mit nachvollziehbarer Quelle beantwortet.
- Antwort und Audit lassen sich über dieselbe Correlation-ID zuordnen.
- Kein beteiligter Agent besitzt Schreib- oder Ausführungsrechte.
- Mindestens 80 Prozent der Pilotfragen liefern einen brauchbaren Suchansatz; die genaue Qualitätsmetrik wird in I0 definiert.

---

## 13. I7 – Sessions, Branches und Git-Worktrees

Status: geplant  
Ziel: PatchPony kann isolierte, zeitlich begrenzte Arbeitskopien erzeugen und sicher verwerfen.

### 13.1 Arbeitspakete

- [x] `I7.1` Session-Domänenmodell und Zustandsübergänge vervollständigen.
- [x] `I7.2` Session-Verzeichnislayout definieren.
- [x] `I7.3` Git-Worktree auf serverseitig erzeugtem Branch anlegen.
- [x] `I7.4` Branch- und Sessionnamen ausschließlich serverseitig erzeugen.
- [x] `I7.5` Projekt-, Branch- und Pfad-Locks implementieren.
- [x] `I7.6` maximale Sessiondauer und Workspace-Größe durchsetzen.
- [x] `I7.7` Status und Diff einer Session bereitstellen.
- [x] `I7.8` explizites Verwerfen einer Session implementieren.
- [x] `I7.9` automatisches Cleanup abgelaufener Sessions implementieren.
- [x] `I7.10` Crash-Recovery für angelegte Worktrees implementieren.
- [x] `I7.11` Schutz gegen Symlink- und Worktree-Ausbruch erneut auf RW-Pfaden testen.

### 13.2 Fähigkeiten

```text
session.create
session.describe
session.close
changes.diff
changes.discard
```

I7 erlaubt noch kein allgemeines Schreiben durch einen Agenten. Sessions können erzeugt und verwaltet werden, um den Lifecycle zu härten, bevor `config.patch` hinzukommt.

### 13.3 Tests

- parallele Sessions desselben Projekts
- doppelte Session-Anlage mit Idempotenzschlüssel
- Branch-Namenskollision
- abgelaufene Session
- Prozessabbruch während Worktree-Anlage
- manueller Verlust eines Sessionverzeichnisses
- Disk-Quota und maximale Dateianzahl
- Verwerfen beschädigt weder Base-Checkout noch andere Sessions

### 13.4 Security Gate B1

- Base-Checkout bleibt unverändert.
- Session kann vollständig und deterministisch entfernt werden.
- Kein Agent kann Sessionpfad oder Branchname frei bestimmen.
- Kein Session-Mount enthält Git- oder Host-Credentials.

### 13.5 Abnahmekriterien

- Sessions sind voneinander und vom Base-Checkout isoliert.
- Cleanup ist wiederholbar und crash-sicher.
- Alle Session- und Branch-Aktionen sind auditiert.

---

## 14. I8 – Config-Patches und Validierung

Status: geplant  
Ziel: Erlaubte XML-, JSON- und YAML-Dateien können kontrolliert in einer Session verändert und validiert werden.

### 14.1 Patch-Modell

Ein Patch-Auftrag enthält mindestens:

- Session-ID
- Zielpfad
- erwartete Prüfsumme der Ausgangsdatei
- strukturierte Änderung oder streng angewendeten Patch
- erwarteten Dateityp

Patches werden ohne unscharfes Matching angewendet. Weicht die Ausgangsdatei von der erwarteten Prüfsumme ab, wird der Auftrag abgelehnt und muss neu geplant werden.

### 14.2 Arbeitspakete

- [x] `I8.1` Config-Adapter-Schnittstelle definieren.
- [x] `I8.2` JSON-Adapter mit Parser und JSON-Schema-Validierung implementieren.
- [x] `I8.3` YAML-Adapter mit Parser, Alias-/Anchor-Limits und Schema-Validierung implementieren.
- [x] `I8.4` XML-Adapter mit sicherer Parserkonfiguration und optionaler XSD-Validierung implementieren.
- [x] `I8.5` externe Entities und DTDs im XML-Parser deaktivieren.
- [x] `I8.6` Patch-Anwendung mit Pfad-, Hash- und Größenprüfung implementieren.
- [x] `I8.7` Write-Policy für erlaubte und verbotene Pfade durchsetzen.
- [x] `I8.8` Diff mit Redaction und Größenlimit erzeugen.
- [x] `I8.9` automatische Rücknahme bei Parser- oder Schemaverletzung implementieren.
- [x] `I8.10` MCP- und REST-Werkzeuge `config.patch` und `config.validate` bereitstellen.
- [x] `I8.11` Rolle `config-editor` projektbezogen aktivierbar machen.

### 14.3 Grenzen für automatische Änderungen

- ausschließlich im Manifest freigegebene Config-Pfade
- keine Secrets-Dateien
- keine Source-Dateien
- keine erzeugten Dateien
- maximale Anzahl Dateien und Diff-Zeilen
- keine neuen symbolischen Links
- Datei bleibt nach Änderung parsebar
- Schema und Projektregeln bleiben gültig

### 14.4 Tests

- gültige und ungültige JSON-, YAML- und XML-Patches
- XXE- und Entity-Expansion-Versuche
- YAML-Alias-Bomb
- Hash-Konflikt durch zwischenzeitliche Änderung
- Patch außerhalb des erlaubten Pfads
- Symlink als Ziel
- sehr großer Patch
- Secret-ähnliche Werte im Diff und Log
- atomarer Rollback bei fehlgeschlagener Validierung

### 14.5 Security Gate B2

Config-Schreibrechte dürfen erst aktiviert werden, wenn:

- Pfad-, Symlink- und Parser-Security-Tests grün sind,
- jeder Patch auf eine Session begrenzt ist,
- Diff und Rollback funktionieren,
- weder Commit noch Push möglich sind.

### 14.6 Abnahmekriterien

- Ein Config-Change kann in einer isolierten Session vorbereitet werden.
- Ungültige Änderungen hinterlassen keine teilweise veränderte Session.
- Der Benutzer erhält einen nachvollziehbaren Diff und Validierungsbericht.
- Es gibt weiterhin keine freie Shell und keine Veröffentlichung ins Remote-Repository.

---

## 15. I9 – Gehärtete Sandbox und registrierte Tests

Status: geplant  
Ziel: Projektdefinierte Validatoren und Tests können kontrolliert in kurzlebigen Containern ausgeführt werden.

### 15.1 Arbeitspakete

- [x] `I9.1` interne Worker-API für Sandbox-Aufträge definieren.
- [x] `I9.2` festen Katalog erlaubter Runner-Images implementieren.
- [x] `I9.3` registrierte `command_id` auf serverseitige Argumentlisten abbilden.
- [x] `I9.4` Docker-Container mit non-root, read-only Root-FS und `cap-drop=ALL` starten.
- [x] `I9.5` `no-new-privileges`, seccomp und AppArmor konfigurieren.
- [x] `I9.6` Netzwerk standardmäßig deaktivieren.
- [x] `I9.7` CPU-, RAM-, PID-, Disk- und Zeitlimits durchsetzen.
- [x] `I9.8` nur den Session-Worktree beschreibbar mounten.
- [x] `I9.9` stdout/stderr mit Byte- und Zeitlimits erfassen.
- [x] `I9.10` Testresultate und Artefaktmetadaten speichern.
- [x] `I9.11` Cancellation und hartes Cleanup implementieren.
- [x] `I9.12` Rootless-Docker-Tauglichkeit für die benötigten Runner prüfen.
- [x] `I9.13` MCP-/REST-Fähigkeiten `tests.list`, `tests.run` und `tests.result` bereitstellen.

### 15.2 Keine freie Shell

Das Projektmanifest referenziert registrierte Kommandos:

```yaml
tests:
  - id: test.billing.unit
    image: dotnet-test-v1
    timeout: 300s
```

Die konkrete Executable und Argumentliste stammen aus einer serverseitigen, versionierten Runner-Definition. Das Modell darf weder Image, Executable noch beliebige Argumente bestimmen.

### 15.3 Security-Tests

- `rm` oder Schreibzugriff außerhalb der Session
- Lesen von Host-, SSH-, Cloud- und Git-Credentials
- Zugriff auf Docker-Socket
- Internet- und DNS-Verbindung
- Fork-Bomb und Prozesslimit
- Endlosschleife und Timeout
- Speicher- und Disk-Erschöpfung
- manipuliertes Runner-Image
- übergroße stdout-/stderr-Ausgabe
- Abbruch während eines Tests

### 15.4 Security Gate C

Ausführungsrechte dürfen erst für den Pilot freigeschaltet werden, wenn:

- der Gateway keinen Docker-Zugriff besitzt,
- der Worker nicht öffentlich erreichbar ist,
- Runner-Images fest freigegeben und gepinnt sind,
- Netzwerk und Host-Zugriff technisch blockiert sind,
- Ressourcenlimits und Cleanup nachgewiesen sind,
- die Security-Testfixtures grün sind.

### 15.5 Release: Change Proposal MVP

Nach I9 kann ein Config-Change vollständig vorbereitet, validiert und getestet werden. Das Ergebnis bleibt lokal in PatchPony und wird als Diff zur manuellen Übernahme ausgegeben.

### 15.6 Abnahmekriterien

- Ein registrierter Validator läuft reproduzierbar in der Sandbox.
- Ein absichtlich bösartiger Test kann die definierten Grenzen nicht überschreiten.
- Testlogs und Ergebnis sind dem Job eindeutig zugeordnet.
- Security Gate C ist dokumentiert bestanden.

---

## 16. I10 – Commit, Push und Merge Request

Status: geplant  
Ziel: Erfolgreich validierte Änderungen können kontrolliert über einen Git-Bot zur menschlichen Review veröffentlicht werden.

### 16.1 Arbeitspakete

- [x] `I10.1` Provider-Abstraktion für GitHub oder GitLab definieren.
- [x] `I10.2` ausschließlich den in I0 gewählten Provider implementieren.
- [x] `I10.3` Bot-/Service-Account mit minimalen Repository-Rechten konfigurieren.
- [x] `I10.4` serverseitige Branch-Namens- und Commit-Message-Vorlagen implementieren.
- [x] `I10.5` `git.status`, `git.create_branch` und `git.commit` implementieren.
- [x] `I10.6` kontrollierten Push nur auf Bot-Branches implementieren.
- [x] `I10.7` Merge-Request-Erstellung über Provider-API implementieren.
- [x] `I10.8` Beschreibung mit Ticket, Plan, Diff-Zusammenfassung, Tests und Risiken erzeugen.
- [x] `I10.9` Reviewer-Zuordnung aus Projektpolicy implementieren.
- [x] `I10.10` menschliche Freigabe vor Push oder MR projektbezogen unterstützen.
- [x] `I10.11` Idempotenz und Retry für Commit, Push und MR sicherstellen.
- [x] `I10.12` Credential-Rotation und Redaction testen.

### 16.2 Unverhandelbare Git-Regeln in V1

- kein Force-Push
- kein Push auf geschützte Branches
- kein Merge durch PatchPony
- kein Löschen fremder Branches
- keine vom Modell bestimmte Remote-URL
- keine Git-Hooks aus dem Repository ausführen
- kein Commit bei fehlgeschlagener Pflichtvalidierung
- jede Veröffentlichung referenziert Job, Session und Audit-Trail

### 16.3 Tests

- Branch-Kollision und idempotente Wiederholung
- Push wird nach Netzwerkfehler sicher wiederholt
- vorhandener Merge Request wird erkannt
- geschützter Branch wird abgelehnt
- manipulierte Remote-URL wird ignoriert beziehungsweise abgelehnt
- Git-Hooks werden nicht ausgeführt
- Token erscheint nicht in Logs, Prozessliste oder Fehlermeldung
- Reviewer- und Approval-Policy wird erzwungen

### 16.4 Security Gate D

Remote-Schreibrechte dürfen erst aktiviert werden, wenn:

- Bot-Rechte auf das notwendige Minimum reduziert sind,
- Branch Protection serverseitig aktiv ist,
- PatchPony technisch nicht mergen kann,
- Commit, Push und MR idempotent sind,
- Approval- und Auditpfad funktionieren.

### 16.5 Abnahmekriterien

- Ein validierter Pilot-Change erzeugt genau einen Branch und einen Merge Request.
- Der Merge Request enthält Plan, Diff, Tests und Risiken.
- Der Merge bleibt vollständig menschlich kontrolliert.

---

## 17. I11 – Zoho-Task-Workflow

Status: geplant  
Ziel: Use-Case 1 funktioniert vom Zoho-Webhook bis zum Kommentar, Plan oder Merge Request.

### 17.1 Arbeitspakete

- [x] `I11.1` Zoho-Webhook authentifizieren und Payload strikt validieren.
- [x] `I11.2` Idempotenzschlüssel aus Ticket, Revision und Eventtyp bilden.
- [x] `I11.3` Ticketdaten in ein internes neutrales Schema normalisieren.
- [x] `I11.4` Projektzuordnung konfigurieren.
- [x] `I11.5` Anhänge mit Größen-, Typ- und Archivlimits behandeln.
- [x] `I11.6` projektspezifische Vollständigkeitskriterien definieren.
- [x] `I11.7` Triage-Ausgabe als strukturiertes Schema implementieren.
- [x] `I11.8` gezielte Rückfrage bei fehlenden Informationen erzeugen.
- [x] `I11.9` Plan aus Skills, Source, Config und freigegebenem Vault-Wissen als strukturiertes Ergebnis erzeugen.
- [x] `I11.10` `plan.md` als lesbare Darstellung rendern.
- [x] `I11.11` Automatisierungsentscheidung ausschließlich anhand harter Policy treffen.
- [x] `I11.12` zulässige Config-Fixes durch Session, Validierung, Tests und MR führen.
- [x] `I11.13` Ergebnis und Links im Zoho-Task kommentieren.
- [x] `I11.14` Fehler- und Eskalationspfade implementieren.

### 17.2 Workflow-Ausgänge

```text
needs_information
plan_only
change_proposal_ready
merge_request_created
manual_investigation_required
failed
```

Das Modell darf eine Empfehlung liefern. Die finale Wahl des Ausgangs erfolgt durch serverseitige Regeln.

### 17.3 Evaluation

- mindestens 20 repräsentative historische Bugreports
- Anteil korrekt erkannter fehlender Informationen
- Qualität der gefundenen Dateien und Tests
- fachliche Brauchbarkeit des Plans
- False-Positive-Rate bei automatisierbaren Änderungen
- Anteil manueller Korrekturen am erzeugten Merge Request

Für automatische Änderungen ist eine niedrige False-Positive-Rate wichtiger als eine hohe Automatisierungsquote.

### 17.4 Tests

- doppelter oder verspäteter Webhook
- Ticket wird während der Analyse geändert
- unbekanntes Projekt
- fehlende Pflichtfelder
- manipulierte Anhänge und Zip-Bomb
- Prompt Injection im Ticket oder Anhang
- fehlgeschlagener Zoho-Kommentar nach erfolgreichem MR
- Wiederaufnahme eines teilweise abgeschlossenen Workflows

### 17.5 Abnahmekriterien

- Ein reales Pilot-Ticket läuft Ende-zu-Ende durch den passenden Ausgang.
- Wiederholte Webhooks erzeugen keine doppelten Kommentare oder Merge Requests.
- Unsichere oder unvollständige Tickets werden nicht automatisch verändert.
- Zoho-Kommentar, PatchPony-Job und Merge Request sind miteinander verknüpft.

---

## 18. I12 – Feature- und Change-Request-Workflow

Status: geplant  
Ziel: Use-Case 2 verwendet dieselbe sichere Pipeline mit eigenen fachlichen Regeln für Config-Changes.

### 18.1 Arbeitspakete

- [x] `I12.1` Tickettypen und gewünschte Änderung strukturiert erfassen.
- [x] `I12.2` Regeln für Development-, Staging- und Production-Konfiguration definieren.
- [x] `I12.3` bekannte Config-Schlüssel, Typen und Wertebereiche prüfen.
- [x] `I12.4` Widersprüche zwischen Einstellungen erkennen.
- [x] `I12.5` Reviewer und Freigaben je Umgebung zuordnen.
- [x] `I12.6` Config-only-Automatisierung auf freigegebene Projekte begrenzen.
- [x] `I12.7` Feature-Requests mit Source-Änderungen auf `plan_only` beschränken.
- [x] `I12.8` n8n-Workflow und Zoho-Kommentare für Change-Requests ergänzen.
- [ ] `I12.9` **← Durchführung offen** historische Change-Requests evaluieren (anonymisierten Export bereitstellen).

### 18.2 Policy-Matrix

| Änderung | V1-Verhalten |
|---|---|
| Development-Config | MR nach erfolgreicher Validierung möglich |
| Staging-Config | MR plus definierter Reviewer |
| Production-Config | explizite menschliche Freigabe vor Veröffentlichung |
| Secrets | immer ablehnen und auf Secret-Prozess verweisen |
| Source-Code | nur Plan und Recherche |
| Datenbankmigration | nur Plan und manuelle Umsetzung |

### 18.3 Zwischenstand: operative Workflows

Nach I12 sind die drei operativen Entwicklungs-Use-Cases abgedeckt:

- Entwicklerfragen read-only
- Bugreport-Triage und Planung
- kontrollierte Config-Changes bis zum Merge Request

Der vierte Use-Case – die kontrollierte Pflege des Git-versionierten Wissensvaults – wird in I13 ergänzt und schließt PatchPony V1 ab.

### 18.4 Abnahmekriterien

- Environment- und Reviewer-Policies werden technisch erzwungen.
- Source-Code- oder Secret-Änderungen können nicht als Config-Change getarnt werden.
- Mindestens ein realer Change-Request wurde erfolgreich bis zur Review vorbereitet.

---

## 19. I13 – Obsidian-Wissensvault lesen und pflegen

Status: geplant  
Ziel: Use-Case 4 funktioniert vom Auffinden firmenweiten Wissens bis zu einer kontrollierten Vault-Änderung als Merge Request.

### 19.1 Arbeitspakete

- [x] `I13.1` Knowledge-Source-Modell für einen separaten Git-Vault und projektbezogene Pfadfreigaben finalisieren.
- [x] `I13.2` `knowledge.tree`, `knowledge.search`, `knowledge.read` und `knowledge.links` als stabile MCP- und REST-Verträge gehärtet (ohne Vault-Inhalte; default-deny).
- [x] `I13.3` Markdown- und Wiki-Link-Parser mit kanonischer Pfadauflösung implementiert (kein Dateisystemzugriff, Traversal- und Plattformpfade abgewiesen).
- [x] `I13.4` YAML-Frontmatter mit Alias-, Tiefen- und Größenlimits sowie optionalem serverseitigem Schema validiert.
- [x] `I13.5` Attachment-Allowlist und Einzel-, Gesamt- sowie Anzahlgrenzen implementiert (default-deny).
- [x] `I13.6` `.obsidian/plugins/**`, `.obsidian/snippets/**`, Skripte und ausführbare Inhalte zentral technisch gesperrt.
- [x] `I13.7` Semantische `knowledge.patch`-Proposal-Operationen für freigegebene Markdown- und Frontmatter-Änderungen implementiert (ohne Dateischreibzugriff).
- [x] `I13.8` Reine Link- und Backlink-Auswirkungsanalyse für Umbenennungen und Änderungen implementiert (ohne Vault-Zugriff).
- [x] `I13.9` Serverseitige Knowledge-Owner- und unabhängige Reviewer-Zuordnung pro Vault-Bereich konfigurierbar gemacht (default-deny).
- [x] `I13.10` Vault-Änderungen über aktive Session, Bot-Branch, Diff/Linkprüfung, Commit, menschlich gegateten Push und Merge Request geführt (kein Merge).
- [x] `I13.11` Open-WebUI-Workflow für Wissensfragen und explizite Pflegeaufträge ergänzt; Pflegeaufträge erzeugen ausschließlich einen read-only recherchierten Entwurf.
- [x] `I13.12` Versionierten, datenfreien Evaluationssatz für reale Pilot-Wissensfragen, kleine Pflegeentwürfe und Sicherheitsgrenzen angelegt; Ausführung bleibt bis zur Auswahl freigegebener Vault-Beispiele gesperrt.
- [x] `I13.13` Inhaltssicheren, reviewer-geschützten Knowledge-Audit-Trail für Leseoperationen, Linkauswirkungen sowie aufgelöste Owner/Reviewer ergänzt; Pfade und Personen bleiben prozessgebundene Fingerprints.

### 19.2 Grenzen für automatische Wissensänderungen

- nur freigegebene `.md`- und Metadatenpfade
- keine direkte Änderung auf geschützten Branches
- kein automatisches Merge
- keine Plugin-, Skript- oder CSS-Ausführung
- keine Massenverschiebungen oder Massenumbenennungen
- keine Löschung ohne explizite Freigabe
- keine gebrochenen internen Links
- gültiges Frontmatter und Einhaltung projektbezogener Wissensregeln
- kleine, nachvollziehbare Diffs mit benanntem fachlichen Reviewer

### 19.3 Tests

- Wiki-Link- und Markdown-Link-Traversierung außerhalb des Vault-Bereichs
- zirkuläre Links und sehr große Backlink-Mengen
- YAML-Alias-Bomb und übergroßes Frontmatter
- eingebettetes HTML, Skriptblöcke und `data:`-URLs
- manipulierte oder ausführbare Attachments
- gleichzeitige Änderungen derselben Vault-Seite
- Umbenennung mit Linkbruch
- fehlender Knowledge Owner
- Prompt Injection in einer Vault-Seite
- Branch-, Commit- und Merge-Request-Idempotenz

### 19.4 Security Gate E

Vault-Schreibrechte dürfen erst aktiviert werden, wenn:

- Pfad-, Link-, Frontmatter- und Attachment-Tests grün sind,
- Plugins und ausführbare Inhalte technisch ausgeschlossen sind,
- jede Änderung in einer isolierten Session erfolgt,
- Knowledge Owner und menschliche Review erzwungen werden,
- PatchPony weiterhin nicht mergen kann.

### 19.5 Release: PatchPony V1

Nach I13 sind alle vier primären Use-Cases abgedeckt:

- Entwicklerfragen read-only
- Bugreport-Triage und Planung
- kontrollierte Config-Changes bis zum Merge Request
- Wissensvault lesen und kontrolliert über Merge Requests pflegen

### 19.6 Abnahmekriterien

- Eine reale Wissensfrage wird mit nachvollziehbaren Vault-Quellen beantwortet.
- Eine kleine Dokumentationsänderung erzeugt genau einen validierten Merge Request.
- Ein Link- oder Frontmatter-Fehler verhindert die Veröffentlichung.
- Der Merge bleibt vollständig menschlich kontrolliert.

---

## 20. I14 – Produktionshärtung und Pilotbetrieb

Status: geplant  
Ziel: PatchPony kann für ein begrenztes Pilotprojekt mit definiertem Betrieb, Monitoring und Recovery eingesetzt werden.

### 20.1 Arbeitspakete

- [x] `I14.1` Reproduzierbare Debian-13-Hostbaseline mit explizitem SSH-Sicherheitsgate, automatischen Security-Updates, AppArmor, auditd und Zeitsynchronisation angelegt; Ausführung und Evidenz erfolgen bewusst erst auf dem Produktionshost.
- [x] `I14.2` Dedizierten Rootless-Docker-Daemon für den privaten Sandbox-Worker mit cgroup-v2-Gate, eigener Systemd-User-Unit und Socket-/Mount-Minimierung vorbereitet; rootful Docker und Docker-Group-Zugang bleiben ausgeschlossen.
- [x] `I14.3` Default-drop nftables-Edge-Policy, getrennte Compose-Edge-/Daten-Netze, Loopback-only Rootless-Gateway und nativen Caddy-HTTPS-Proxy vorbereitet; nur SSH sowie 80/443 sind als Host-Eingang vorgesehen.
- [x] `I14.4` Isolierte Backup-/Restore-Rehearsal für PostgreSQL erstellt und geprüft.
- [x] `I14.5` Kontrollierten Rotationsablauf für Service- und Bot-Credentials mit Validierung, lokaler Rücknahme und Betriebschecks erstellt.
- [x] `I14.6` Privates Prometheus-/Grafana-Dashboard und Alarmregeln für Jobs, Fehler, Queue, Sessions und Datenbankressourcen bereitgestellt.
- [x] `I14.7` Dry-run-first, batchbegrenzte Daten- und Log-Retention mit täglichem Rootless-Timer bereitgestellt.
- [x] `I14.8` Dependency-Review, transitive NuGet- und Container-Scans sowie CycloneDX-SBOM-Artefakte in CI finalisiert.
- [x] `I14.9` Reproduzierbare, lokale k6-Last- und Parallelitätstests inklusive Rate-Limit-Schutzszenario vorbereitet; echte Pilotausführung bleibt evidenzpflichtig.
- [x] `I14.10` Evidenzbasiertes Security-Review gegen das versionierte Threat Model durchgeführt; Pilot-Blocker ausdrücklich dokumentiert.
- [x] `I14.11` Incident-, Recovery- und Kill-Switch-Runbooks mit reversiblem Gateway-/Worker-Containment erstellt.
- [ ] `I14.12` **← freigabe- und evidenzpflichtig** kontrollierten Pilot mit kleiner Benutzergruppe durchführen; Runbook und Gates vorbereitet.
- [ ] `I14.13` **← wartet auf I14.12-Evidenz** Pilotmetriken auswerten und V1.1-Backlog erstellen; datensparsame Auswertungsstruktur vorbereitet.

### 20.2 Betriebsmetriken

- Jobs nach Ausgang und Projekt
- Triagezeit und Gesamtlaufzeit
- Queue-Wartezeit
- aktive und abgelaufene Sessions
- Sandbox-Ressourcenverbrauch
- fehlgeschlagene Validatoren und Tests
- Policy-Denials
- erzeugte und angenommene Merge Requests
- manuelle Korrekturen an PatchPony-Merge-Requests
- Wissensfragen, Quellenabdeckung und angenommene Vault-Merge-Requests
- wiederholte und fehlerhafte Webhooks

### 20.3 Notfallfunktionen

- globales Deaktivieren aller schreibenden Tools
- projektweises Deaktivieren von PatchPony
- Stoppen und Verwerfen aktiver Sessions
- Sperren des Git-Bot-Tokens
- Pausieren der Worker-Queue
- Umschalten auf ausschließlich read-only ohne Redeployment

### 20.4 Abnahmekriterien

- Restore wurde praktisch getestet, nicht nur dokumentiert.
- Ein Kill Switch deaktiviert Schreib- und Git-Fähigkeiten sofort.
- Security-Review besitzt keine offenen kritischen Befunde.
- Pilotbetrieb hat definierte Eigentümer, Alarmwege und Supportzeiten.
- V1.1-Backlog basiert auf gemessenen Ergebnissen.

---

## 21. Test- und Freigabematrix

| Fähigkeit | Früheste Iteration | Erforderliches Gate |
|---|---:|---|
| lokales Projekt lesen | I3 | Pfad- und Symlink-Tests |
| Wissensvault lesen | I3 | Pfad-, Link- und Attachment-Tests |
| MCP read-only intern | I4 | MCP- und Contract-Tests |
| MCP read-only extern | I5 | Security Gate A |
| Open-WebUI-Pilot | I6 | Read-only-Evaluation |
| Session anlegen | I7 | Security Gate B1 |
| Config schreiben | I8 | Security Gate B2 |
| Tests ausführen | I9 | Security Gate C |
| Remote-Branch schreiben | I10 | Security Gate D |
| Zoho-Automation | I11 | Idempotenz- und Attachment-Tests |
| Config-Change-Automation | I12 | Environment- und Reviewer-Policy |
| Wissensvault schreiben | I13 | Security Gate E |
| Produktion | I14 | Security Review, Restore und Runbooks |

---

## 22. Durchgängige technische Regeln

Diese Regeln gelten in jeder Iteration:

### 22.1 Prozessaufrufe

- kein `bash -c`, `sh -c`, `cmd /c` oder PowerShell mit Modellinput
- Executable serverseitig festlegen
- Argumente einzeln über `ProcessStartInfo.ArgumentList`
- Arbeitsverzeichnis serverseitig kanonisieren
- Environment explizit aufbauen
- Timeout und Cancellation verwenden
- stdout und stderr begrenzen

### 22.2 Dateisystem

- keine absoluten Pfade aus API-Eingaben
- kanonische Pfade vor Policy-Prüfung
- Symlinks explizit behandeln
- atomare Writes über temporäre Datei im selben Dateisystem
- Ausgangs-Hash bei Änderungen prüfen
- Dateigröße und Dateityp begrenzen
- Markdown-, Wiki- und Attachment-Links vor Nutzung kanonisch auflösen
- Obsidian-Plugins und ausführbare Inhalte nie ausführen

### 22.3 Datenbank

- Migrationen sind versioniert und Bestandteil der CI
- Idempotenzschlüssel besitzen Unique Constraints
- Zustandsübergänge erfolgen transaktional
- Audit-Ereignisse sind append-only
- Locks haben Eigentümer und Ablaufzeit

### 22.4 APIs

- Default-Deny
- stabile Fehlercodes
- keine internen Stacktraces an Clients
- Request- und Response-Limits
- Cancellation weiterreichen
- Versionierung und Contract-Tests
- Authentifizierung ersetzt keine fachliche Autorisierung

### 22.5 Agenten und Modelle

- Modelloutput ist untrusted
- Toolparameter werden vollständig validiert
- Skills, Repository-Inhalte und Vault-Seiten können keine Policy überschreiben
- maximale Toolaufrufe und Agentenschritte
- keine Secrets im Prompt
- Providerfreigabe pro Projekt

---

## 23. Hauptrisiken und Gegenmaßnahmen

| Risiko | Gegenmaßnahme | Primäre Iteration |
|---|---|---:|
| zu großer Anfangsscope | ein Pilotprojekt und ein Git-Provider | I0 |
| Sicherheitslogik verteilt sich auf Controller | transportunabhängiger Core | I2–I4 |
| Pfad- oder Symlink-Ausbruch | kanonische Pfadabstraktion und Security-Tests | I3 |
| MCP wird zu früh öffentlich | externe Freigabe erst nach Security Gate A | I5 |
| Prompt Injection aktiviert Aktionen | Capability- und Policy-Prüfung außerhalb des Modells | durchgängig |
| Session beschädigt Base-Checkout | Worktree-Isolation und Cleanup-Tests | I7 |
| Config-Parser wird angegriffen | sichere Parserkonfiguration und Größenlimits | I8 |
| Docker-Socket kompromittiert Host | Gateway/Worker-Trennung und interner Worker | I9 |
| Git-Bot besitzt zu viele Rechte | Bot-Branch-only und Branch Protection | I10 |
| Webhook erzeugt doppelte MRs | persistente Idempotenz | I2, I11 |
| schlechte Agentenantworten | historischer Evaluationsdatensatz | I6, I11, I12, I13 |
| Vault-Inhalt enthält Prompt Injection oder unsichere Obsidian-Erweiterung | Vault als untrusted Data, Plugin-Sperre und Policy außerhalb des Modells | I3, I13 |
| Wissensänderung bricht Links oder verwischt Verantwortlichkeiten | Linkanalyse, Knowledge Owner und menschliche Review | I13 |
| zu viele Technologien | C#-Control-Plane, Python nur ergänzend | durchgängig |

---

## 24. Backlog nach V1

Die folgenden Themen sind ausdrücklich nicht Teil der ersten Version:

- automatische Source-Code-Änderungen
- automatisches Mergen
- zweiter Git-Provider
- Language-Server-Integration
- semantische Codeindizes oder Embeddings
- semantischer Vault-Index oder Knowledge Graph mit Embeddings
- automatische Massenverschiebungen, Massenumbenennungen und Wissensbereinigung
- mehrere Worker und horizontale Skalierung
- dediziertes Queue-System
- allgemeines Sandbox-Netzwerk
- feingranulare Netzwerk-Allowlist
- gVisor, Kata Containers oder MicroVMs
- projektspezifische Source-Code-Implementierungsagenten
- automatische Review-Kommentare auf fremden Merge Requests
- eigene Weboberfläche neben Open WebUI und n8n
- MCP Apps oder andere experimentelle MCP-Erweiterungen

Diese Punkte werden erst priorisiert, wenn der Pilot reale Engpässe oder einen klaren Nutzen zeigt.

---

## 25. Empfohlener nächster Schritt

Die Umsetzung beginnt nicht direkt mit MCP oder Docker-Sandboxing, sondern mit I0.

Für den ersten Arbeitsblock werden diese acht Entscheidungen benötigt:

1. Welches konkrete Repository wird Pilotprojekt?
2. Liegt es auf GitHub oder GitLab?
3. Welcher Identity Provider ist vorhanden?
4. Welches Modell darf den Sourcecode dieses Projekts sehen?
5. Welche Config-Dateitypen und Testbefehle müssen für den ersten realen Change unterstützt werden?
6. In welchem Git-Repository liegt der Obsidian-Vault?
7. Welche Vault-Pfade sind für den Pilot lesbar beziehungsweise später schreibbar?
8. Wer übernimmt fachliche Ownership und Review für diese Wissensbereiche?

Danach kann I1 ohne weitere Architekturentscheidung umgesetzt werden.
