# Knowledge Sources

Ein Wissensvault ist eine eigene, projektgebundene Git-Quelle und nie eine Ableitung des Projekt-Repositories. Pro Projekt kann zunächst genau ein Vault mit dem festen Namen `vault` registriert werden.

Bei der Registrierung werden nur `https`- und `ssh`-Remotes sowie sichere Branch-Namen akzeptiert. Die Datenbank speichert Quelle, Remote, Default-Branch und Registrierungszeit in `patchpony.knowledge_sources`.

Vor jeder Nutzung erstellt oder aktualisiert der kontrollierte Checkout-Service einen eigenen Clone pro Knowledge Source. Er holt ausschließlich den registrierten Branch, checkt ihn detached aus und gibt die aufgelöste Commit-ID zurück. Nachgelagerte Vault-Lese- und Suchfunktionen müssen diese Commit-ID als Revision referenzieren; sie dürfen weder einen Branch-Namen noch das Projekt-Repository als Wissensstand verwenden.

Eine konkrete Vault-Remote wird erst bei der späteren Projektkonfiguration registriert. Dadurch enthält weder die Konfiguration im Repository noch diese Dokumentation Zugangsdaten.
