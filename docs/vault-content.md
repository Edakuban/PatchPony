# Vault-Inhalte

Alle Vault-Operationen nehmen ein `KnowledgeSourceCheckout` entgegen und liefern dessen immutable Git-Commit-Revision mit zurück. Damit bleibt für aufrufende Workflows nachvollziehbar, auf welchem Wissensstand Baum, gelesene Datei und Suchtreffer beruhen.

`KnowledgeVaultService` verwendet ausschließlich den serverseitig verwalteten Checkout der `KnowledgeSourceId`. Relative Pfade werden gegen diesen Root aufgelöst; Traversal, absolute Pfade und Reparse Points werden abgewiesen.

| Operation | Grenze |
| --- | --- |
| Baum | Tiefe 5, 500 untersuchte Einträge, Dateien bis 1 MiB |
| Markdown-Reader | ausschließlich `.md`, UTF-8 ohne Binärdaten, 128 KiB und 500 Zeilen |
| Suche | ausschließlich Markdown, 200 Kandidaten, 128 KiB je Datei, 256 Zeichen Query, 20 Treffer pro Datei und 100 insgesamt |

Die Suche ist eine literale, nicht ausführende Volltextsuche und unterscheidet nicht zwischen Groß- und Kleinschreibung. Übersprungene, zu große oder über das Limit hinausgehende Inhalte markieren das Ergebnis als abgeschnitten.
