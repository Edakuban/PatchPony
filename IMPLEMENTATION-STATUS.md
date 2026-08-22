# PatchPony – Umsetzungsstand

Letzte Aktualisierung: 2026-08-22  
Aktive Iterationen: I0, I1, I2 und I3

Dieses Dokument ergänzt [PLAN.md](PLAN.md). Es hält den tatsächlichen
Implementierungsstand fest, bis die jeweilige Iteration abgeschlossen ist.

## Nächster konkreter Schritt

`I4.8` – Cancellation und Timeouts bis in Datei- und Suchoperationen weiterreichen.

## I0 – Entscheidungen und Projektvorbereitung

| Paket | Status | Stand |
|---|---|---|
| I0.1 Pilotprojekt | erledigt | PatchPony |
| I0.2 Git-Provider | erledigt | GitHub: `Edakuban/PatchPony` |
| I0.3 Branch-Strategie | offen | Für den MVP: `main` schützen, Arbeit nur über Feature-Branches und Pull Requests. Umsetzung auf GitHub folgt. |
| I0.4 Identity | teilweise erledigt | Ein lokales Entwicklungspasswort; produktiver Identity Provider steht noch aus. |
| I0.5 n8n-Servicekonto | offen | Erst nötig, wenn n8n integriert wird. |
| I0.6 Modellprovider | teilweise erledigt | destination.one, lokal gehostetes `gpt-oss:20b`; Datenschutzgrenze noch festzuhalten. |
| I0.7 Deployment-Topologie | offen | Lokal zuerst; Produktions-Topologie folgt vor Pilotbetrieb. |
| I0.8 Projektsprachen/Testumgebung | offen | PatchPony selbst: .NET 10 und xUnit. |
| I0.9 Aufbewahrung | offen | Vor persistenter Job- und Audit-Datenhaltung entscheiden. |
| I0.10 Threat Model | offen | Vor Freischaltung von Projektzugriff abschließen. |
| I0.11 ADR-Ordner/Template | erledigt | `docs/adr/`; erste ADR angelegt. |
| I0.12 Backlog/Labels | offen | GitHub-Labels folgen vor externem Workflow. |
| I0.13–I0.14 Wissensvault | vorbereitet | Read-only vorgesehen; Vault existiert noch nicht. |

## I1 – Solution, Build und lokale Infrastruktur

| Paket | Status | Stand |
|---|---|---|
| I1.1 Repository | erledigt | Lokales Git-Repository, `origin` auf GitHub-Repository. |
| I1.2 Solution und Projekte | erledigt | .NET-10-Solution mit Core, Contracts, Infrastructure, Gateway, Worker und CLI. |
| I1.3 Testprojekte | erledigt | Core-, Integration-, Policy- und Security-Testprojekt angelegt. |
| I1.4 Build-Konfiguration | erledigt | `Directory.Build.props`, `.editorconfig`, `.gitignore` und zentrale Paketverwaltung vorhanden. |
| I1.5 Compiler-Leitplanken | erledigt | Nullable, deterministische Builds und Warnings-as-Errors aktiviert. |
| I1.6 Gateway-Health | erledigt | `/health/live` und `/health/ready` sowie WebApplicationFactory-Integrationstests vorhanden. |
| I1.7 Worker | erledigt | Worker als .NET-`BackgroundService` und eigener non-root-Container vorhanden. |
| I1.8 Dockerfiles | erledigt | Gateway und Worker: Multi-Stage, non-root. |
| I1.9 Docker Compose | erledigt | PostgreSQL, Gateway und Worker sind lokal gebaut, gestartet und melden alle `healthy`; Gateway-Probes liefern `200`. |
| I1.10 Lokale Konfiguration | erledigt | `.env.example` dokumentiert, `.env` zuverlässig ignoriert. |
| I1.11 CI | erledigt | GitHub Actions für Restore, Release-Build, Tests und beide Container-Builds angelegt. |
| I1.12 Scans | erledigt | NuGet-Audit für direkte und transitive Abhängigkeiten sowie Trivy-Scans der Gateway- und Worker-Images in CI. |

## I2 – Domänenmodell, Persistenz und Job-Lifecycle

| Paket | Status | Stand |
|---|---|---|
| I2.1 Aggregate und Value Objects | erledigt | Transportfreier Core für Projekte, Repositoryregistrierung, Jobs, Sessions, Approvals, Artefakte, Toolinvocations und Idempotenzrecords. |
| I2.2 Job-Zustandsmaschine | erledigt | Explizite erlaubte und verbotene Übergänge mit Statusänderungsereignissen und Unit-Tests. |
| I2.3 Fehlercodes und Ergebnisse | erledigt | Stabile `validation.invalid`, `job.transition.invalid` und `job.transition.noop`-Ergebnisse. |
| I2.4 PostgreSQL und Migration | erledigt | EF Core 10 mit Npgsql, `patchpony`-Schema, Mapping für die I2-Entitäten sowie versionierte Initialmigration. |
| I2.5 Application Services und Repositories | erledigt | Core-Ports und EF-Repositoryadapter für Projekte, Jobs und Sessions; Application Services erzwingen Projekt- und Lifecycle-Regeln. |
| I2.6 Idempotenzservice | erledigt | Atomarer PostgreSQL-Adapter legt Job und Idempotenzrecord zusammen an; derselbe Schlüssel liefert den ursprünglich angelegten Job. |
| I2.7 Datenbankbasierte Job-Queue | erledigt | Persistente `job_queue` mit Fälligkeit, eindeutiger Job-Referenz und geordneter Ready-Abfrage; Claiming folgt separat. |
| I2.8 Job-Claiming | erledigt | PostgreSQL-Claim mit `FOR UPDATE SKIP LOCKED`, Claim-Token, Worker-ID und ablaufender Lease; abgelaufene Jobs können neu geclaimt werden. |
| I2.9 Audit-Schnittstelle | erledigt | Validierter Core-Port und EF-Adapter hängen Ereignisse ausschließlich an; PostgreSQL-Trigger blockiert Updates und Deletes. |
| I2.10 Correlation-ID | erledigt | Gateway übernimmt/generiert `X-Correlation-ID`, Core verwaltet transportfreie Scopes, Audit schreibt die aktive ID und Worker setzt eigene Verarbeitungs-Scopes. |
| I2.11 Retention | erledigt | Reine Vorschau abgelaufener Sessions und Idempotenzrecords; kein automatischer oder verfügbarer Löschpfad. |
## I3 – Projektkatalog und sichere Read-only-Runtime

| Paket | Status | Stand |
|---|---|---|
| I3.1 Projektmanifest und JSON Schema | erledigt | Version-1-Schema, dokumentierte Sicherheitsregeln und ein vollständiges Beispiel für `.patchpony/project.yaml` angelegt. |
| I3.2 Manifest-Deserialisierung und Schema-Validierung | erledigt | Begrenzter, fail-closed YAML-Loader blockiert Aliase/Anchors/Tags und unbekannte Felder; JSON Schema wird aus einer eingebetteten Ressource ausgewertet. |
| I3.3 Projektregistrierung | erledigt | Manifest-ID wird eindeutig gespeichert; Repository-URL und Default-Branch werden atomar mitregistriert. |
| I3.4 kontrollierter Base-Checkout | erledigt | Git wird ohne Shell mit festen Argumenten ausgeführt; nur registrierte HTTPS/SSH-Remotes und der registrierte Branch werden geklont/fetched. |
| I3.5 Repository-Revisionen | erledigt | Der Base-Checkout löst nach Detached Checkout `HEAD^{commit}` auf und gibt ausschließlich eine validierte vollständige Commit-ID zurück. |
| I3.6 kanonische Pfadauflösung | erledigt | Relative Pfade werden an einem serverseitigen Checkout-Root kanonisch aufgelöst; Fehler geben keine Host-Pfade preis. |
| I3.7 Pfad- und Symlink-Härtung | erledigt | `..`, absolute Pfade und Backslashes werden verworfen; Links müssen mit ihrem finalen Ziel im Checkout bleiben. |
| I3.8 Manifest-Pfad-Policy | erledigt | Glob-Policy wertet `readable`, `writable` und `forbidden` fail-closed aus; der I3-Base-Checkout bleibt technisch read-only. |
| I3.9 Skill-Katalog und Skill-Reader | erledigt | Fester read-only Skill-Pfad, Policy-/Symlink-Prüfung, ID-Validierung sowie UTF-8- und Größenlimits ohne Ausführung von Inhalten. |
| I3.10 Projektbaum | erledigt | Read-only Baum mit Policy-Prüfung, Tiefen-, Eintrags- und Dateigrößenlimits; Symlink-Verzeichnisse werden nicht traversiert. |
| I3.11 Source-Suche | erledigt | Literal-`rg` mit festen Argumenten, Policy-geprüften Kandidaten und Datei-, Treffer-, Ausgabe- sowie Zeitlimits. |
| I3.12 Source-Reader | erledigt | Policy-geprüfter UTF-8-Reader mit 128-KiB- und 500-Zeilen-Limit; Binär- und ungültige Dateien werden abgewiesen. |
| I3.13 Lokale CLI | erledigt | `project validate` und `project diagnose` validieren Manifest und Read-only-Baum ohne Schreib-, Git- oder Netzwerkausführung. |
| I3.14 Knowledge Source | erledigt | Separat persistierter, projektgebundener `vault` mit kontrolliertem Git-Checkout und unveränderlicher Commit-Revision. |
| I3.15 Vault-Inhalte | erledigt | Revisionsgebundener, sicherer Baum sowie Markdown-Reader und literale Suche mit festen Pfad-, Größen- und Trefferlimits. |
| I3.16 Vault-Links | erledigt | Rein textuelle Wiki-/Markdown-Linkauflösung und revisionsgebundene Backlinks ohne Plugin- oder Netzwerkausführung. |
| I4.1 MCP C# SDK | erledigt | Offizielles `ModelContextProtocol.AspNetCore` 2.2.0 zentral gepinnt und MCP-Serverdienste im Gateway registriert. |
| I4.2 MCP über HTTP | erledigt | Stateless Streamable-HTTP-Transport unter `POST /mcp`, einschließlich Aushandlungstest. |
| I4.3 REST-Versionierung | erledigt | Stabile `/api/v1`-Route-Group mit Discovery-Endpunkt; unversionierte API-Routen bleiben ausgeschlossen. |
| I4.4 MCP-Tools | erledigt | `runtime.status` als read-only, idempotenter MCP-Adapter auf den Core-Korrelationskontext; Discovery und Aufruf getestet. |
| I4.5 REST für n8n | erledigt | `GET /api/v1/runtime/status` ist der getestete REST-Gegenpart zu `runtime.status` und nutzt denselben Core-Handler. |
| I4.6 API-Fehler | erledigt | Domainfehler werden transportübergreifend mit Code, Message und Korrelations-ID abgebildet; REST-Status und MCP-`isError` getestet. |
| I4.7 Gateway-Limits | erledigt | Feste Target-, Body-, Ergebnis- und gemeinsame Parallelitätslimits für MCP und REST mit einheitlichen Fehlern. |
| I4.8 | offen – als Nächstes | Cancellation und Timeouts bis in Datei- und Suchoperationen weiterreichen. |

## Sicherheitsgrenze

Bis I2 abgeschlossen ist, besitzt PatchPony keine Projekt-, Schreib-, Shell-
oder Git-Operation für Agenten. Die aktuell angelegten Komponenten sind nur
das lokale Build- und Betriebsfundament.
