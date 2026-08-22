# Vault-Links

`KnowledgeVaultLinkService` interpretiert Vault-Markdown rein textuell. Es führt keine Obsidian-Plugins aus, lädt keine externen Ziele und bewertet keine eingebetteten Skripte.

Unterstützt werden Wiki-Links wie `[[Guide]]` und `[[Guide#Abschnitt|Alias]]` sowie Markdown-Links wie `[Guide](guide.md#abschnitt)`. Relative Markdown-Ziele werden nur innerhalb des Vaults normalisiert. Wiki-Links ohne Pfad können eindeutig über ihren Markdown-Dateinamen aufgelöst werden. Nicht vorhandene, mehrdeutige oder aus dem Vault führende Ziele bleiben ungelöst.

`http`, `https` und `mailto` werden lediglich als externe Links markiert. Sie werden niemals aufgerufen. Bilder in Markdown (`![…](…)`) sind keine navigierbaren Vault-Links.

Backlinks werden durch kontrolliertes Einlesen der bekannten Markdown-Dateien ermittelt. Link- und Dateilimits werden als `IsTruncated` sichtbar gemacht; alle Resultate tragen die konkrete Git-Commit-Revision des verwendeten Vault-Checkouts.
