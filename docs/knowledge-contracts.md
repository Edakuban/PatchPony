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