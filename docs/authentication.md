# Authentifizierung

## Benutzer-JWTs über OIDC

PatchPony registriert das Bearer-Schema `PatchPony` mit dem offiziellen
ASP.NET-Core-JWT-Validator. Für einen produktiven Identity Provider werden
nur diese Konfigurationswerte benötigt:

```text
PATCHPONY_AUTH__OIDC__AUTHORITY=https://identity.example.com
PATCHPONY_AUTH__OIDC__AUDIENCE=patchpony-api
PATCHPONY_AUTH__OIDC__REQUIREHTTPSMETADATA=true
```

Der Validator nutzt die OIDC-Metadaten der `Authority` und prüft damit
Signatur, Aussteller, Audience und Ablaufzeit. Fehlen Authority oder Audience,
verwirft der Bearer-Validator Tokens fail-closed. Die konkreten Werte gehören
zur noch offenen Identity-Provider-Entscheidung und werden nicht eingecheckt.

## Lokaler Entwicklungszugang

Ausschließlich mit `ASPNETCORE_ENVIRONMENT=Development` akzeptiert PatchPony
zusätzlich den Header `X-PatchPony-Development-Password`. Sein Wert wird mit
`PATCHPONY_AUTH__DEVELOPMENTPASSWORD` verglichen und nie geloggt. Außerhalb
von Development ist dieses Schema nicht erfolgreich; es ersetzt weder OIDC
noch produktive Benutzerverwaltung.

`GET /api/v1/runtime/identity` ist der erste geschützte Nachweisendpunkt. Er
liefert nur `subject` und `authenticationMode`. Fehlendes, falsches oder
ungültiges Bearer-Material liefert `401`.

Die bestehenden read-only Runtime-Endpunkte und MCP-Aufrufe werden erst mit
I5.4 systematisch auf projekt-, tool- und parameterbezogene Policies gelegt.
Rollen, Scopes und der n8n-Service-Account folgen in I5.2 und I5.3.
## n8n-Service-Account

n8n verwendet kein Benutzerpasswort und keinen Benutzer-JWT. Stattdessen wird
ein separater Secret-Wert über `PATCHPONY_AUTH__N8N__TOKEN` hinterlegt und als
`X-PatchPony-Service-Token` gesendet. Ein gültiger Wert erzeugt ausschließlich
die Identität `service-n8n` mit dem Modus `service-token`; der Vergleich erfolgt
konstantzeitig und der Secret-Wert wird nicht geloggt.

Fehlt die Konfiguration oder ist der Header ungültig, ist die Authentifizierung
fail-closed. Das Token verleiht aktuell keine Rollen, Projekt- oder Toolrechte:
diese Grenzen folgen mit I5.3 und I5.4.
## Rollen und Scopes

Die folgenden Rollen sind als unveränderliche Vertragswerte registriert:
`code-reader`, `issue-planner`, `knowledge-reader`, `knowledge-editor`,
`config-editor`, `reviewer` und `service-n8n`.

Scopes folgen dem Format `domäne:aktion`, darunter `project:read`,
`skills:read`, `knowledge:read`, `source:read`, `config:read`,
`config:write`, `tests:run`, `changes:write` und die `git:*`-Scopes.

Im aktuellen Read-only-Stand leiten Rollen ausschließlich lesende Scopes ab.
Ein `config-editor` erhält daher etwa `config:read`, aber ausdrücklich nicht
`config:write`; `knowledge-editor` erhält kein `knowledge:write`. Eingehende
OIDC-Claims dürfen `role` oder `roles` und `scope` oder `scp` verwenden.

Für jede Rolle und jeden Scope ist eine benannte ASP.NET-Core-Policy vorhanden
(`role:<rolle>` bzw. `scope:<scope>`). Ihre Anwendung auf konkrete Projekte,
MCP-Tools und Parameter folgt erst in I5.4.
## Projekt-, Tool- und Parameterautorisierung

`POST /api/v1/projects/{projectId}/access` ist ein geschützter, read-only
Preflight für die freigegebenen künftigen Projekt-Tools. Er prüft gemeinsam:

- exakte Projektfreigabe als `project`- oder `projects`-Claim;
- den zum Tool gehörenden Scope;
- bekannte Tool-IDs und nur deren erlaubte, begrenzte Parameter.

Der Gateway kennt derzeit `project.tree`, `skills.*`, `source.*` und
`knowledge.*`; jede Tool-ID ist fest einem Read-Scope zugeordnet. Unbekannte
Tools, fremde Projekte, fehlende Scopes oder zusätzliche/fehlende Parameter
werden mit `authorization.forbidden` und HTTP 403 abgelehnt.

Für lokale Tests können Projekte explizit und kommasepariert gesetzt werden:

```text
PATCHPONY_AUTH__DEVELOPMENTPROJECTS=demo-project
PATCHPONY_AUTH__N8N__PROJECTS=demo-project
PATCHPONY_AUTH__DEVELOPMENTCONFIGEDITORPROJECTS=demo-project
```

Produktive OIDC-Tokens müssen eigene `project`- beziehungsweise `projects`-
Claims enthalten. Es gibt keine Projekt-Wildcard. Sobald Source- und
Knowledge-Tools als MCP-/REST-Fähigkeiten veröffentlicht werden, verwenden sie
denselben `ProjectToolAuthorizationService` vor dem Fachhandler.
## Default-Deny

Die globale Fallback-Policy verlangt Authentifizierung für jede Route, die
nicht ausdrücklich freigegeben wurde. `/api/v1` und `/mcp` sind damit auch bei
neuen Endpunkten automatisch geschützt. Öffentlich bleiben ausschließlich die
Health-Probes (`/health/live`, `/health/ready`), die Root-Information und im
Development-Modus die OpenAPI-/Swagger-Hilfe.

In Production ist Swagger UI nicht registriert; anonyme Anfragen werden vorher
durch Default-Deny mit `401` abgewiesen. Das OpenAPI-Dokument benötigt dort
ebenfalls Authentifizierung.

## Interne Worker-Authentifizierung

Queue-Claims für Worker sind unabhängig von Benutzer-JWTs und n8n-Token mit
einem eigenen HMAC-SHA-256-Schlüssel signiert. Der Proof bindet Claim, Job,
Worker-ID, Ausgabezeit und Lease-Ablauf; der Worker prüft ihn konstantzeitig,
gegen seine eigene ID und gegen die Ablaufzeit. Konfiguration und
Verarbeitungsgrenze sind in [worker-authentication.md](worker-authentication.md)
dokumentiert.
## Audit von Zugriffsentscheidungen

Das Gateway protokolliert für `/api/v1` und `/mcp` eine strukturierte
Authentifizierungs- und globale Policyentscheidung (`allowed` oder `rejected`). Der
Projekt-Tool-Preflight protokolliert zusätzlich die kombinierte
`policy.tool`-Entscheidung mit Projekt-ID, Tool-ID und benötigtem Scope.
Jeder Eintrag trägt die Correlation-ID, den Subjektwert und den
Authentifizierungsmodus.

Nicht aufgezeichnet werden Header, Bearer-Token, Entwicklungskennwörter,
n8n-Service-Tokens, Query-Strings oder Toolparameterwerte. Die letzten 1.000
Ereignisse sind ausschließlich für die Rolle `reviewer` unter
`GET /api/v1/runtime/audit/access?limit=100` einsehbar und werden zusätzlich
strukturiert geloggt. Der In-Memory-Auszug ist absichtlich flüchtig; eine
spätere produktive Persistenz muss denselben Redaction-Vertrag einhalten.