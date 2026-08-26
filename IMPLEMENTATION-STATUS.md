# PatchPony – Umsetzungsstand

Letzte Aktualisierung: 2026-08-23
Aktive Iterationen: I0 bis I11

Dieses Dokument ergänzt [PLAN.md](PLAN.md). Es hält den tatsächlichen
Implementierungsstand fest, bis die jeweilige Iteration abgeschlossen ist.

## Nächster konkreter Schritt

`I13.7` – `knowledge.patch` als semantisches Werkzeug für freigegebene Markdown- und Frontmatter-Änderungen implementieren.

## I7 – Sessions, Branches und Git-Worktrees

| Paket | Status | Stand |
|---|---|---|
| I7.1 | erledigt | Persistierte Session-Zustandsmaschine für Provisionierung, Aktivierung, Abschluss, Ablauf und Fehler mit stabilen Übergangsfehlern. |
| I7.2 | erledigt | Deterministisches, ausschließlich serverseitiges Layout unter einem dedizierten Storage-Root; noch ohne Dateisystem- oder Git-Aktion. |
| I7.3 | erledigt | Kontrollierter lokaler `git worktree add -b`-Aufruf für Provisioning-Sessions, mit festem Branchformat, Timeout und deaktivierten Hooks. |
| I7.4 | erledigt | Gemeinsame Core-Policy erzeugt Directory- und Branchnamen ausschließlich aus der serverseitigen Session-ID. |
| I7.5 | erledigt | Dateibasierte, cross-process Lease-Locks für serverseitig abgeleitetes Projekt, Branch und Worktree-Pfad; nur der Eigentümer mit Lease-ID kann freigeben, abgelaufene Locks werden atomar übernommen. |
| I7.6 | erledigt | Harte 8-Stunden-Grenze beim Erzeugen von Sessions sowie serverseitige, symlinkfreie Größenmessung eines neuen Worktrees mit 1-GiB-Limit. |
| I7.7 | erledigt | Persistierter Session-Status über den Application Service und begrenzter, ausschließlich serverseitig abgeleiteter Git-Diff für aktive oder schließende Sessions. |
| I7.8 | erledigt | Expliziter Discard persistiert zuerst Closing, entfernt unter Lease-Locks nur abgeleiteten Worktree und Branch und schließt erst nach erfolgreicher Bereinigung. |
| I7.9 | erledigt | Batchbegrenzter, retry-sicherer Ablauf-Cleanup markiert Fälligkeit, verwendet den kontrollierten Discard und schließt nur nach erfolgreicher Bereinigung. |
| I7.10 | erledigt | Wiederaufnahme unvollständiger Provisioning- oder Closing-Sessions über denselben kontrollierten Discard; unbekannte Ordner bleiben unangetastet. |
| I7.11 | erledigt | Gemeinsamer Reparse-Point-Guard schützt Worktree-Anlage, Diff, Größenmessung, Lease-Speicher und kontrollierte Löschung; reale Symlink-Regressionstests vorhanden. |
## I8 – Config-Patches und Validierung

| Paket | Status | Stand |
|---|---|---|
| I8.1 | erledigt | Formatneutrale, read-only Adaptergrenze mit serverkomponiertem Katalog, strukturierten Validierungsberichten und begrenzten Schema-Referenzen. |
| I8.2 | erledigt | Strikter JSON-Parser mit 1-MiB-Grenze, Tiefenlimit und optionaler Validierung über einen fest registrierten JsonSchema.Net-Katalog. |
| I8.3 | erledigt | Strikter YAML-Eventscan mit Anchor-/Alias-Limit 0, Node-Limit, tag- und Mehrfachdokument-Sperre sowie optionaler Schema-Validierung. |
| I8.4 | erledigt | Begrenzter XML-Reader und optionaler, ausschließlich serverseitig registrierter und kompilierter XSD-Katalog. |
| I8.5 | erledigt | DTDs und externe Entities werden vor dem Reader abgewiesen; Reader und XSD-Katalog nutzen zusätzlich DTD-Prohibit, null Resolver und Entity-Limits, mit XXE-Regressionstest. |
| I8.6 | erledigt | Atomare Full-Content-Patches für vorhandene Config-Dateien mit sicherer Pfadauflösung, Dateityp-, SHA-256- und 1-MiB-Prüfung. |
| I8.7 | erledigt | Patch-Anwendungen benötigen eine passende `writable`-Regel; `forbidden` hat Vorrang und verhindert Änderungen zuverlässig. |
| I8.8 | erledigt | Hash-gepinnte Unified-Diff-Vorschau mit Secret-Redaction, Write-Policy und festen Zeilen-/UTF-8-Ausgabelimits. |
| I8.9 | erledigt | Validierte Patches werden bei Parser- oder Schemafehler atomar zurückgenommen; Hash-Konflikte schützen nachträgliche Änderungen. |
| I8.10 | erledigt | Serverkonfigurierte Session-Kataloge sowie abgesicherte REST- und MCP-Werkzeuge für Validierung und validierte Patches. |
| I8.11 | erledigt | Projektclaim `project_role=<projekt>:config-editor` leitet Schreibscope ab; `config.patch` erzwingt die Rolle zusätzlich zum Scope. |

## I9 – Gehärtete Sandbox und registrierte Tests

| Paket | Status | Stand |
|---|---|---|
| I9.1 | erledigt | Private, replay-geschützte Worker-API mit signiertem Worker-Claim, Session-ID und registrierbarer Test-ID; keine Ausführungsparameter im Contract. |
| I9.2 | erledigt | Geschlossener serverseitiger Runner-Katalog mit eindeutigen IDs und ausschließlich sha256-digest-gepinnten Image-Referenzen. |
| I9.3 | erledigt | Geschlossener Command-Katalog ordnet IDs fest registrierten Runnern, Executables und Argumentlisten zu; Shells sind ausgeschlossen. |
| I9.4 | erledigt | Worker startet ausschließlich serverkomponierte Docker-Aufrufe mit non-root UID, read-only Root-FS und `cap-drop=ALL`. |
| I9.5 | erledigt | Verpflichtende Docker-Sicherheitsoptionen: no-new-privileges, vorhandenes serverseitiges Seccomp-Profil und validiertes AppArmor-Profil. |
| I9.6 | erledigt | Jeder Sandbox-Container verwendet fest `--network none`; keine Eingabe kann diese Default-Deny-Regel überschreiben. |
| I9.7 | erledigt | Serverseitig validierte CPU-, RAM-, PID- und Disk-Grenzen werden als Docker-Limits gesetzt; ein Watchdog beendet den Docker-Prozessbaum nach dem Zeitlimit. |
| I9.8 | erledigt | Der einzige schreibbare Docker-Bind-Mount ist der serverseitig abgeleitete, vorhandene und symlinkfreie Session-Worktree unter `/workspace`; bei Unsicherheit startet kein Container. |
| I9.9 | erledigt | Docker stdout und stderr werden parallel ohne Shell erfasst, pro Stream serverseitig bytebegrenzt und bei Ablauf des gemeinsamen Ausführungszeitlimits als begrenzt markiert. |
| I9.10 | erledigt | Beendete Sandbox-Läufe speichern Ergebnisstatus, Exit-Code und begrenzte Ausgabe als serverseitige Session-Datei; Metadaten sicherer Artefakte werden aus dem festen Worktree-Unterordner ergänzt. |
| I9.11 | erledigt | Cancellation und Timeout beenden den Docker-Prozessbaum und entfernen den serverseitig benannten Container gezielt; temporäre Ergebnisdateien werden auch bei Schreibfehlern bereinigt. |
| I9.12 | erledigt | Worker prüft beim Start einen Rootless-Docker-Daemon; jeder digest-pinnte Runner benötigt zusätzlich eine explizite Rootless-Kompatibilitätsfreigabe. |
| I9.13 | erledigt | MCP und REST bieten katalogisierte Testliste, serverseitig validierten privaten Worker-Handoff und begrenzten Ergebnisabruf; der Gateway erhält keinen Docker-Zugriff. |

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
| I4.8 | erledigt | `SourceReader.ReadAsync`: fünf Sekunden Deadline und Caller-Cancellation; Suche: Cancellation vor und während der Kandidatensuche sowie im `rg`-Prozess. |
| I4.9 | erledigt | MCP-Discovery-Vertrag testet die vollständige Toolmenge, Namen, Titel, Sicherheitsannotationen sowie Input-/Output-Schemas. |
| I4.10 | erledigt | Streamable-HTTP-Initialize und Capabilities sind automatisiert geprüft; lokaler MCP-Inspector-Smoke-Test ist dokumentiert. |
| I4.11 | erledigt | OpenAPI unter `/openapi/v1.json`; Swagger UI nur im Development-Modus, Dokument- und Umgebungsgrenzen automatisiert getestet. |

## I5 – Authentifizierung und Autorisierung

| Paket | Status | Stand |
|---|---|---|
| I5.1 | erledigt | OIDC/JWT-Bearer-Schema mit Authority/Audience-Konfiguration, fail-closed ohne Providerwerte und Development-Passwort nur im lokalen Modus. |
| I5.2 | erledigt | Separater `X-PatchPony-Service-Token`-Handler für `service-n8n`, nur aus Konfiguration, fail-closed und mit konstantzeitlichem Secret-Vergleich. |
| I5.3 | erledigt | Zentrale Rollen-/Scope-Verträge, Role- und Scope-Policies sowie ausschließlich lesende Scope-Vererbung für lokale und n8n-Identitäten. |
| I5.4 | erledigt | Geschützter Projekt-Tool-Preflight erzwingt exakte Projektclaims, zugeordnete Scopes und strikt erlaubte, begrenzte Parameter. |
| I5.5 | erledigt | Globale Fallback-Policy sperrt API und MCP ohne explizite Freigabe; nur Health, Root und lokale Entwicklungsdoku bleiben anonym. |
| I5.6 | erledigt | Signierter `WorkerClaimProof` bindet Claim, Job, Worker-ID, Ausgabe- und Ablaufzeit; HMAC-Prüfung, Worker-Bindung und Lease-Ablauf sind getestet. Der Worker liest eine eigene, fail-closed konfigurierte Base64-32-Byte-Signatur. |
| I5.7 | erledigt | Begrenzter, reviewer-geschützter Auszug und strukturierte Logs erfassen Authentifizierung, globale Policies sowie `policy.tool`-Entscheidungen mit Correlation-ID, ohne Credentials oder Parameterwerte. |
| I5.8 | erledigt | Caddy terminiert TLS; der Gateway ist nur im Compose-Netz erreichbar. Lokale interne CA, Produktions-Overlay mit automatischen Zertifikaten und CI-Parserprüfung sind dokumentiert. |
| I5.9 | erledigt | Begrenzter Fixed-Window-Store schützt REST und MCP pro Subjekt/Gegenstelle; separates Kontingent, sofortiges 429 mit Retry-After, Health-Ausnahme und Flood-Schutz der Partitionstabelle sind getestet. |
| I5.10 | erledigt | CORS ist standardmäßig deaktiviert und lässt nur explizit konfigurierte HTTP(S)-Origins zu; Methoden, Request-Header und Response-Header sind eng begrenzt, Credentials werden nicht geteilt. |
| I5.11 | erledigt | Zentrale Redaction maskiert Tokens, Credentials, Secrets sowie Ticket-/Issue-Daten vor Audit-Speicherung und Logausgabe. |
| I5.12 | erledigt | Acht negative Projekt-, Tool-, Scope- und Parameterfälle laufen als dedizierte Policy-Testmatrix; Wire- und Sicherheitsfälle sind dokumentiert zugeordnet. |

## I6 – Open WebUI, n8n und Entwicklerfragen

I6-Pilotentscheidungen: [docs/i6-pilot-decisions.md](docs/i6-pilot-decisions.md).

| Paket | Status | Stand |
|---|---|---|
| I6.1 | erledigt | Importierbare Open-WebUI-Pipe leitet ausschließlich die letzte Benutzerfrage über einen konfigurierbaren internen n8n-Webhook weiter und erwartet eine minimale Antwortstruktur. |
| I6.2 | erledigt | Eigenes `service-n8n`-Credential wird fail-closed aus Deployment-Secrets geladen, ist auf Projekt-IDs begrenzt und lehnt mehrdeutige Headerwerte ab; n8n-Setup ist dokumentiert. |
| I6.3 | erledigt | Importierbarer, zunächst inaktiver n8n-Agentenworkflow validiert die Pipe-Payload, nutzt `gpt-oss:20b` und erlaubt über MCP ausschließlich die beiden vorhandenen Runtime-Read-Tools. |
| I6.4 | erledigt | Nur die globale Admin-Projektbindung wird weitergegeben; die Pipe erzeugt ein pseudonymes Open-WebUI-Subjekt und n8n validiert Projekt, Identitätsformat und Anfrage erneut. |
| I6.5 | erledigt | Projektgebundene MCP-Tools `source.search` und `source.read` liefern zitierfähige Pfad- und Zeilenangaben für `patchpony` und `vocavid`. |
| I6.6 | erledigt | n8n gibt ausschließlich verifizierte Source-Zitate und erlaubte Toolnamen zurück; die Pipe zeigt sie sicher unter der Antwort. |
| I6.7 | erledigt | Der n8n-Agent ist auf sechs Iterationen und damit maximal sechs MCP-Toolaufrufe pro Anfrage begrenzt. |
| I6.8 | erledigt | Zeitlimit, Iterationslimit, Abbruch und Überlastung liefern sichere deutsche Nutzertexte ohne interne Fehlerdetails. |
| I6.9 | erledigt | 15 versionierte PatchPony-/VocaVid-Fragen mit erwarteten Quellen, Sicherheitsgegenproben und manueller Auswertung sind angelegt. |
| I6.10 | technisch erledigt | Datenflussinventar, Datenminimierung und offene formale Freigabebedingungen für Open WebUI, n8n, Gateway und destination.one dokumentiert. |
| I6.11 | vorbereitet | Acht Vault-Fragen samt Sicherheitsgegenproben sind versioniert; Ausführung ist bis zur Vault-Registrierung bewusst gesperrt. |
## Sicherheitsgrenze

Bis I2 abgeschlossen ist, besitzt PatchPony keine Projekt-, Schreib-, Shell-
oder Git-Operation für Agenten. Die aktuell angelegten Komponenten sind nur
das lokale Build- und Betriebsfundament.

## I10 – Commit, Push und Merge Request

| Paket | Status | Stand |
|---|---|---|
| I10.1 | erledigt | Providerneutraler Contract für das idempotente Finden und Erstellen eines Merge Requests; GitHub ist anhand des Projektremotes als einziger Implementierungsprovider festgelegt. |
| I10.2 | erledigt | GitHub-REST-Adapter sucht offene Pull Requests und erstellt neue ausschließlich aus dem validierten Provider-Draft; Repository-URIs sind auf github.com begrenzt. |
| I10.3 | erledigt | GitHub-Token ist ein nicht logbares, auf eine serverseitige github.com-Repository-URI gebundenes Secret; Dokumentation definiert fein granulierte minimale Rechte und Secret-Ablage. |
| I10.4 | erledigt | Branchname und Commit-Message werden ausschließlich aus übereinstimmenden serverseitigen Projekt-, Job- und Sessiondaten abgeleitet. |
| I10.5 | erledigt | Kontrollierter Status und Commit auf dem serverseitigen Session-Branch; Hooks sind deaktiviert und Commit-Text sowie Pfad werden nicht aus Clientdaten abgeleitet. |
| I10.6 | erledigt | Push prüft die registrierte HTTPS-Origin und veröffentlicht ausschließlich den serverseitigen Session-Branch mit explizitem Ref-Spec, ohne Force oder Tags. |
| I10.7 | erledigt | Merge-Request-Service sucht zuerst einen offenen GitHub-Pull-Request für denselben Session-Branch und erstellt nur bei Bedarf einen neuen; keine Merge-Funktion vorhanden. |
| I10.8 | erledigt | Feste, begrenzte PR-Beschreibung mit Ticket, Plan, Diff-Zusammenfassung, Tests und Risiken; keine rohe Ausgabe oder freien Git-Parameter. |
| I10.9 | erledigt | Geschlossene Projektpolicy ordnet validierte GitHub-Reviewer zu; der Provider ruft Reviewer-Zuordnung nur mit diesen serverseitigen Werten auf. |
| I10.10 | erledigt | Projektpolicy kann Push und Merge Request freigabepflichtig machen; nur die jüngste explizit genehmigte Job-Freigabe öffnet das Fail-Closed-Gate. |
| I10.11 | erledigt | Serverseitiger Operationsschlüssel, maximal drei Retries nur für normalisierte transiente Fehler; Commit prüft den festen Head-Text, Push bleibt auf den nicht-forcierten Session-Ref begrenzt und MR sucht vor jedem Create erneut. |
| I10.12 | erledigt | Atomarer, repositorygebundener Credential-Snapshot wechselt kontrolliert zum neuen Token; Headerrotation und GitHub-/OAuth-spezifische Redaction sind mit künstlichen Secrets getestet. |

## I11 – Zoho-Task-Workflow

| Paket | Status | Stand |
|---|---|---|
| I11.1 | erledigt | Signierter, anonymer Zoho-Task-Webhook mit HMAC-SHA-256 über den Raw-Body, 64-KiB-Limit, geschlossenem Task-Schema und sicheren, inhaltslosen Fehlern. |
| I11.2 | erledigt | Versionierter SHA-256-Schlüssel aus kanonischem Eventtyp, Ticket-ID und Revision; inhaltsfrei, stabil für Retries und unterschiedlich bei Revision/Eventwechsel. |
| I11.3 | erledigt | Gateway mappt das geschlossene Zoho-Format in ein erneut validiertes, providerneutrales Core-Ticket mit Quelle, Event, ID, Revision, Text und Empfangszeit. |
| I11.4 | erledigt | Geschlossene, serverkonfigurierte Ticketprefix-zu-Manifest-ID-Zuordnung; unbekannte oder mehrdeutige Projekte werden fail-closed vor jeder Folgeaktion abgewiesen. |
| I11.5 | erledigt | Geschlossene Attachment-Metadaten: maximal fünf erlaubte Text-/Config-Dateien, 2 MiB je Datei und 5 MiB gesamt; Pfadtricks, unbekannte Typen und Archive sind fail-closed gesperrt. |
| I11.6 | erledigt | Serverseitige Policy pro Manifest-Projekt prüft Mindestbeschreibung, erlaubte Anhänge und erforderliche Textmerkmale; Ergebnis sind nur stabile Missing-Criteria-IDs. |
| I11.7 | erledigt | Inhaltsarmes, versionierbares Triage-Schema verbindet Ticketreferenz, Projekt, Idempotenz, Vollständigkeitskriterien und deterministische Disposition ohne Modell- oder Freigabeentscheidung. |
| I11.8 | erledigt | Deterministische, begrenzte deutsche Rückfrageentwürfe werden ausschließlich aus bekannten Missing-Criteria-IDs gebildet; kein Modell und kein Zoho-Kommentaraufruf. |
| I11.9 | erledigt | Review-only-Planvertrag mit validierten Skill-, Source-, Config- und Knowledge-Referenzen sowie drei festen Phasen; keine freien Pfade, Tools oder Schreibaktionen. |
| I11.10 | erledigt | Deterministischer, nur lesender Renderer erzeugt feste `plan.md`-Markdowndarstellung aus validierter Evidenz und Planphasen, ohne Ticketinhalte oder Dateischreiboperation. |
| I11.11 | erledigt | Harte, serverkonfigurierte Outcome-Policy: nur explizit erlaubte Config-only-Pläne mit geforderter Evidenz werden `change_proposal_ready`; Source-Evidenz bleibt immer `plan_only`. |
| I11.12 | erledigt | Geschlossener Orchestrator führt nur `change_proposal_ready` strikt über Session, validierten Patch, registrierte Tests, Freigabe und MR; Policy- oder Zwischenfehler stoppen vor weiteren Nebenwirkungen. |
| I11.13 | erledigt | Sicherer, begrenzter Task-Kommentaradapter rendert validierte Ergebnisse und HTTPS-Links und stellt sie mit OAuth ausschließlich an konfigurierte Zoho-Task-Routen zu; Konfigurations-, Transport- und Providerfehler bleiben inhaltslos. |
| I11.14 | erledigt | Fail-closed Fehlerklassifikation erlaubt Retries nur für bekannte transiente Infrastrukturcodes; Eskalationsdatensätze enthalten ausschließlich Referenzen und Reason-Code, nie Ticketinhalte oder Secrets. |

## I12 – Feature- und Change-Request-Workflow

| Paket | Status | Stand |
|---|---|---|
| I12.1 | erledigt | Typisierter, begrenzter Änderungsauftrag akzeptiert nur Feature- und Change-Request-Tasks, sichere Zielreferenzen sowie optionale Sollwerte; Secret-Sollwerte und Ausführungsdaten sind ausgeschlossen. |
| I12.2 | erledigt | Unüberschreibbare Environment-Matrix: Development nach Validierung, Staging mit Reviewer, Production zusätzlich mit expliziter menschlicher Freigabe vor Veröffentlichung; nur Configuration-Aufträge können sie erhalten. |
| I12.3 | erledigt | Serverseitige, projekt- und umgebungsgebundene Allowlist validiert bekannte Config-Schlüssel als Boolean, Integer, String oder Enum mit Wertebereichen; unbekannte oder fehlerhafte Policies stoppen fail-closed. |
| I12.4 | erledigt | Serverseitige Konfliktregeln erkennen verbotene Paare bereits key-validierter Sollwerte je Projekt und Umgebung; Konflikte und fehlerhafte Regeln stoppen fail-closed ohne Werte offenzulegen. |
| I12.5 | erledigt | Staging und Production erhalten ausschließlich aus serverseitigen Projekt-/Umgebungspolicies konkrete GitHub-Reviewer; Production trägt zusätzlich die explizite Approval-Pflicht, fehlende oder doppelte Regeln stoppen fail-closed. |
| I12.6 | erledigt | Zusätzliche, default-deny Projekt-Allowlist erlaubt Config-only-Automatisierung nur bei genau einer explizit aktivierten lokalen Projektregel; fehlende, doppelte oder deaktivierte Regeln blockieren. |
| I12.7 | erledigt | Harte, channel-basierte Prioritätsregel erzwingt für Feature-Requests mit SourceCode stets `plan_only`; weder Evidenz noch Projektflags können daraus eine Änderungsausführung machen. |
| I12.8 | erledigt | Deaktiviertes, importierbares n8n-Intake-Template validiert ausschließlich strukturierte Config-Change-Requests; eine feste statusorientierte Kommentarvorlage übergibt an den separaten Zoho-Adapter, ohne n8n Zoho- oder PatchPony-Secrets zu geben. |
| I12.9 | Durchführung offen | Versionierter, datensparsamer Satz mit neun Policy-Kategorien und klaren Blockern ist vorbereitet; für die fachliche Abnahme fehlen mindestens zehn anonymisierte abgeschlossene Change-Request-Fälle aus Zoho. |

## I13 – Obsidian-Wissensvault lesen und pflegen

| Paket | Status | Stand |
|---|---|---|
| I13.1 | erledigt | Separater Knowledge-Source-Vertrag plus default-deny, projekt- und sourcegebundene Vault-Pfad-Policies für Read/Write; noch ohne reale Registrierung, Checkout oder Inhalt. |
| I13.2 | erledigt | Stabile, scope-geschützte MCP- und REST-Verträge für Tree, Search, Read und Links; ohne registrierte Vault-Quelle bewusst `knowledge.unavailable` (default-deny). |
| I13.3 | erledigt | Reiner Markdown-/Wiki-Link-Parser löst nur gegen den übergebenen Vault-Katalog kanonisch auf; Traversal-, Backslash-, Laufwerks-, Query- und Kontrollzeichenpfade bleiben ungelöst. |
| I13.4 | erledigt | Initiales YAML-Frontmatter ist auf 32 KiB, 200 Zeilen und 12 Ebenen begrenzt; Anker/Aliasse und Tags werden abgewiesen, optionale Schemas stammen ausschließlich aus dem Serverkatalog. |
| I13.5 | erledigt | Default-deny für Nicht-Markdown-Dateien: nur PNG, JPG/JPEG, WEBP, GIF und PDF; 8 MiB je Datei, maximal 100 Dateien und 64 MiB gesamt. Die Vault-Auflistung blendet abgewiesene Dateien aus. |
| I13.6 | erledigt | Zentrale Content-Policy sperrt Obsidian-Plugins und -Snippets schon vor der Rekursion sowie Skript- und ausführbare Endungen bei List, Read und Search. |
| I13.7 | nächster Schritt | `knowledge.patch` als semantisches Werkzeug für freigegebene Markdown- und Frontmatter-Änderungen implementieren. |
