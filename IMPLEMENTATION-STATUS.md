# PatchPony – Umsetzungsstand

Letzte Aktualisierung: 2026-08-22  
Aktive Iterationen: I0 und I1

Dieses Dokument ergänzt [PLAN.md](PLAN.md). Es hält den tatsächlichen
Implementierungsstand fest, bis die jeweilige Iteration abgeschlossen ist.

## Nächster konkreter Schritt

`I1.6` – Gateway-Health-Endpunkte implementieren und testen. Danach kann der
erste lokale Docker-Compose-Start verifiziert werden.

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
| I1.4 Build-Konfiguration | teilweise erledigt | `Directory.Build.props`, `.editorconfig` und `.gitignore` vorhanden; zentrale Paketverwaltung ist noch offen. |
| I1.5 Compiler-Leitplanken | erledigt | Nullable, deterministische Builds und Warnings-as-Errors aktiviert. |
| I1.6 Gateway-Health | offen – als Nächstes | Health-Endpunkte und zugehörige Tests implementieren. |
| I1.7 Worker | teilweise erledigt | Worker-Skeleton als `BackgroundService` vorhanden. |
| I1.8 Dockerfiles | erledigt | Gateway und Worker: Multi-Stage, non-root. |
| I1.9 Docker Compose | teilweise erledigt | PostgreSQL, Gateway und Worker beschrieben; Health-Checks folgen mit I1.6. |
| I1.10 Lokale Konfiguration | erledigt | `.env.example` dokumentiert, `.env` zuverlässig ignoriert. |
| I1.11 CI | offen | GitHub Actions für Restore, Build, Test, Container-Build. |
| I1.12 Scans | offen | Dependency- und Image-Scanning. |

## Sicherheitsgrenze

Bis I1 abgeschlossen ist, besitzt PatchPony keine Projekt-, Schreib-, Shell-
oder Git-Operation für Agenten. Die aktuell angelegten Komponenten sind nur
das lokale Build- und Betriebsfundament.
