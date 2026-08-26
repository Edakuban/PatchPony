# Knowledge-API-Verträge

Die vier Knowledge-Leseoperationen sind bereits als stabile Adapter-Verträge vorhanden. Sie verarbeiten **keine** reale Vault-Quelle, bis diese in einer späteren Iteration ausdrücklich registriert und freigegeben wird.

| Fähigkeit | MCP-Tool | REST-Endpunkt |
| --- | --- | --- |
| Baum | `knowledge.tree` | `GET /api/v1/projects/{projectId}/knowledge/tree?path=&depth=` |
| Suche | `knowledge.search` | `GET /api/v1/projects/{projectId}/knowledge/search?query=` |
| Seite lesen | `knowledge.read` | `GET /api/v1/projects/{projectId}/knowledge/read?path=` |
| Links lesen | `knowledge.links` | `GET /api/v1/projects/{projectId}/knowledge/links?path=` |

Alle vier Operationen sind read-only und idempotent. Sie verlangen Projektberechtigung und den Scope `knowledge:read`. Ohne konfigurierte und für das Projekt freigegebene Knowledge-Quelle schlagen sie absichtlich mit dem gemeinsamen Fehlercode `knowledge.unavailable` fehl. Der Fehler enthält weder Dateipfade noch Vault-Inhalte.

Die Antwortformen sind bereits fixiert: Baum-Einträge enthalten Pfad, Art und optionale Größe; Suchtreffer Pfad, Zeile und Text; Leseantworten Revision, Pfad, Zeilen und Trunkierungsflag; Links Quelle, Zeile, Art, optionales Ziel und Fragment sowie External-Markierung. Die spätere Vault-Anbindung füllt genau diese Verträge, ohne die öffentliche Schnittstelle zu ändern.
## Linkauflösung

`KnowledgeMarkdownLinkParser` ist ein reiner Parser: Er erhält Markdown-Zeilen und einen bereits begrenzten Katalog von Vault-Dateien, greift aber weder auf das Dateisystem noch auf das Netzwerk zu. Wiki-Links (`[[Ziel#Abschnitt|Alias]]`) und normale Markdown-Links werden gegen diesen Katalog kanonisch aufgelöst. Externe `https`, `http`- und `mailto`-Links werden lediglich gekennzeichnet, niemals abgerufen.

Interne Ziele dürfen den Vault nicht verlassen. Absolute Pfade, `..`-Traversal oberhalb des Vaults, Backslashes, Laufwerks-/URI-artige Doppelpunkte, Query-Zeichen und Steuerzeichen werden nicht aufgelöst. Unklare Wiki-Dateinamen bleiben ebenfalls ungelöst. Die Linkanzahl ist auf den Aufrufergrenzwert begrenzt und signalisiert dann Trunkierung.
## YAML-Frontmatter

`KnowledgeFrontmatterParser` wertet ausschließlich einen einleitenden `---`-Block aus; Markdown ohne diesen ersten Marker bleibt unverändert. Der Block muss innerhalb von 200 Zeilen geschlossen sein und darf höchstens 32 KiB UTF-8-Daten sowie 12 YAML-Verschachtelungsebenen enthalten. YAML-Anker, Aliasse, Tags, doppelte Schlüssel und mehrere Dokumente werden durch die strikte YAML-Validierung verworfen.

Ein Schema ist optional. Seine Kennung wird validiert und ausschließlich gegen einen serverseitig zusammengestellten JSON-Schema-Katalog aufgelöst – weder Schema-Text noch Schema-Pfade sind Teil eines externen Aufrufs.
## Anhänge

Nicht-Markdown-Dateien sind standardmäßig gesperrt. Der reine Metadatenvertrag erlaubt ausschließlich `.png`, `.jpg`, `.jpeg`, `.webp`, `.gif` und `.pdf`; SVG und alle ausführbaren, archivierten oder sonstigen Formate werden nicht in den Vault-Baum aufgenommen. Die Allowlist prüft sichere relative Pfade, maximal 8 MiB pro Datei, höchstens 100 Anhänge und 64 MiB insgesamt. Sie inspiziert oder verarbeitet keinen Binärinhalt.
## Gesperrte Inhalte

Die zentrale Content-Policy läuft vor dem Auflisten, Lesen und Durchsuchen eines Vaults. Sie überspringt `.obsidian/plugins/**` und `.obsidian/snippets/**` vollständig, einschließlich ihrer Verzeichnisrekursion. Zudem bleiben bekannte Skript- und ausführbare Endungen wie `.ps1`, `.sh`, `.js`, `.exe`, `.dll`, `.jar` und `.py` technisch gesperrt. Die Regeln verhindern damit, dass solche Inhalte über einen späteren Knowledge-Adapter sichtbar werden.
## Semantic patch proposals

`KnowledgePatchService` is the implementation behind the planned `knowledge.patch` capability. It accepts a current Markdown value, its SHA-256 revision hash and no more than eight semantic operations: set or remove a simple root frontmatter field, replace one exact Markdown section, or append Markdown. It returns a fully validated in-memory proposal and its next hash; it never writes a file.

Every proposal is restricted to an approved `.md` path and the existing content policy, has a 128 KiB document limit, rejects a stale hash, and revalidates frontmatter after every operation. Raw diffs, arbitrary file replacement, deletion, renaming and bulk changes are intentionally absent. A public write endpoint and filesystem/Git application remain disabled until the isolated session, owner/reviewer and publication controls of later I13 packages are available.
## Link-impact analysis

`KnowledgeLinkImpactAnalyzer` receives at most 200 already supplied Markdown pages and never accesses a vault. For a proposed rename it returns every internal resolved backlink that needs an explicit update. For a proposed content change it returns outgoing links added and removed, all incoming backlinks and incoming fragment links whose referenced heading would disappear. Heading fragments are normalized conservatively for comparison.

The analyzer does not rename files or rewrite links. Its result is an input to the later owner/reviewer and controlled session-publication steps; an affected backlink or broken fragment must block that publication path until explicitly resolved.
## Ownership and review

Before a future write/publication flow can proceed, `KnowledgeAreaOwnershipPolicy` resolves a server-configured area rule for the project and target path. It requires at least one owner and an independent reviewer, prefers the most specific matching glob and fails closed for no match or an equal-specificity tie. The request itself cannot choose either person. Configuration details are in [knowledge-ownership.md](knowledge-ownership.md).

## Session publication

Validated knowledge proposals can now be applied only to an active, server-created session worktree. KnowledgeSessionPublicationWorkflow requires both configured human publication approvals before it mutates that worktree, then uses the controlled bot-branch, commit, push and merge-request services and requests the area reviewers. It has no merge capability. See [knowledge-publication.md](knowledge-publication.md).

## Open WebUI workflow

The **PatchPony · Knowledge** Pipe classifies ordinary prompts as `knowledge-question` and the exact `/knowledge-maintain <request>` prefix as `knowledge-maintenance`. Both intents offer n8n only read-only Source and Knowledge MCP tools. The maintenance intent produces a draft proposal only; it cannot invoke `knowledge.patch`, mutate a worktree, publish Git changes or merge. The full operator contract is in [open-webui-pipe.md](open-webui-pipe.md).

## Evaluation

The versioned I13.12 set contains only templates for approved real pilot samples; it intentionally contains no Vault data and remains `pending-pilot-sample-selection` until the data owner selects a permitted revision. It evaluates citations, proposal-only maintenance intents and safety boundaries separately. See [i13-knowledge-workflow-evaluation.md](i13-knowledge-workflow-evaluation.md).

## Audit

Knowledge reads and session-patch analysis emit only content-free audit metadata. Read and link targets, resolved owners and reviewers use process-scoped fingerprints; link impact is limited to counts and a result code. The reviewer endpoint and retention boundary are described in [knowledge-audit.md](knowledge-audit.md).
