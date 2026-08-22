# PatchPony

![PatchPony logo](https://github.com/Edakuban/PatchPony/blob/main/PatchPony-logo.png)

PatchPony ist eine sichere, projektbezogene Runtime fuer KI-Agenten in der
Softwareentwicklung. Sie verbindet Tickets, freigegebenen Code,
Konfiguration und Unternehmenswissen, ohne dem Modell direkten Zugriff auf
Host, Shell oder geschuetzte Git-Branches zu geben.

> Status: fruehe Implementierung. Das lokale .NET-10-Fundament steht; Projekt-
> und Schreibfaehigkeiten sind noch **nicht** implementiert.

## Zielbild

PatchPony bereitet Aenderungen in einer isolierten Session vor, validiert sie
und stellt sie zur menschlichen Pruefung bereit. Ein automatischer Merge in
geschuetzte Branches ist ausdruecklich kein Ziel von Version 1.

Die erste Ausbaustufe ist vollstaendig read-only:

- Code, Konfiguration und freigegebenes Wissen gezielt durchsuchen und lesen
- Entwicklerfragen ueber MCP oder REST beantworten
- alle Aufrufe und Entscheidungen auditierbar machen

Erst spaetere Stufen erlauben isolierte Config-Patches, registrierte Tests und
Pull Requests.

## Architektur in Kuerze

```text
Open WebUI / n8n
        | MCP oder REST
        v
PatchPony Gateway -- internes Protokoll --> PatchPony Worker
        |                                      |
        +------------ PostgreSQL --------------+
                                               |
                                    temporaere, gehaertete Sandbox
```

- **Gateway:** Authentifizierung, Policies, API, Rate Limits und Audit
- **Worker:** Sessions, Worktrees, Validatoren, Tests und spaeter Diffs
- **PostgreSQL:** Jobs, Sessions, Freigaben und Audit-Metadaten
- **Sandbox:** kurzlebig, non-root, ohne Docker-Socket und standardmaessig ohne
  Netzwerk

Das vollstaendige Konzept steht in
[brainstorm/patchpony-concept.md](brainstorm/patchpony-concept.md); die
Lieferstufen in [PLAN.md](PLAN.md).

## Aktueller Stand

Der fortlaufende, ueberpruefte Arbeitsstand ist in
[IMPLEMENTATION-STATUS.md](IMPLEMENTATION-STATUS.md) dokumentiert.

Bereits vorhanden:

- .NET-10-Solution mit Gateway, Worker, Core, Contracts, Infrastructure und CLI
- Testprojekte fuer Core, Integration, Policies und Security
- lokale `.env`-Konfiguration, die zuverlaessig von Git ignoriert wird
- Dockerfiles fuer Gateway und Worker (Multi-Stage, non-root)
- Docker-Compose-Grundgeruest fuer Gateway, Worker und PostgreSQL
- Modellprovider-Entscheidung fuer das MVP: destination.one mit lokalem
  `gpt-oss:20b`

Noch nicht vorhanden:

- Projektzugriff, Git-Worktrees oder Schreiboperationen fuer Agenten
- Datenmodell und Persistenz
- produktive Authentifizierung
- MCP-, REST- und Modellprovider-Anbindung

## Lokal starten

### Voraussetzungen

- .NET SDK 10
- Docker Desktop oder Docker Engine mit Docker Compose
- Zugang zu einem destination.one-Modellprovider fuer spaetere Modellintegration

### Konfiguration

```powershell
Copy-Item .env.example .env
```

Trage in `.env` mindestens ein lokales Datenbankpasswort ein. Fuer die spaetere
Modellanbindung kommen dazu:

```text
PATCHPONY_AI__ENDPOINT=<dein destination.one API-Endpunkt>
PATCHPONY_AI__APIKEY=<dein API-Schluessel>
PATCHPONY_AI__MODEL=gpt-oss:20b
PATCHPONY_AUTH__DEVELOPMENTPASSWORD=<lokales Entwicklungspasswort>
```

`.env` wird niemals eingecheckt. Bitte keine Schluessel in Tickets, Logs oder
Chatverlaeufe kopieren.

### Build und Tests

```powershell
dotnet restore PatchPony.slnx
dotnet build PatchPony.slnx --no-restore
dotnet test PatchPony.slnx --no-restore
```

### Container

```powershell
docker compose up --build
```

Der Compose-Stack ist aktuell ein Infrastruktur-Skeleton. Die Gateway-
Health-Endpunkte werden als naechster Arbeitsschritt ergaenzt.
