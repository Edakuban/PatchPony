# GitHub-Credential-Rotation und Redaction

Der GitHub-Token liegt ausschließlich in der nicht eingecheckten Deployment-Konfiguration (`PatchPony__GitHub__Token`) oder in einem Secret Store. Er erscheint nie in Commit-Nachrichten, Git-Argumenten, HTTP-Fehlern, Audits oder Logs.

## Rotation

`GitHubProviderCredentialStore` hält einen unveränderlichen Snapshot. Eine Rotation tauscht ihn atomar aus und akzeptiert nur ein Credential für dasselbe registrierte GitHub-Repository. Bereits laufende Provider-Aufrufe verwenden ihren begonnenen Snapshot; der nächste Aufruf verwendet den neuen Token. Eine Rotation kann damit keine Anfrage mit einer halb geschriebenen Konfiguration erzeugen.

Die aktuelle lokale Konfiguration wird nicht automatisch überwacht: Im Deployment wird der neue Secret-Wert zunächst validiert geladen und dann gezielt in den Store übernommen; alternativ ist ein kontrollierter Prozess-Restart zulässig. Alte Tokens erst nach erfolgreichem Reload bzw. Restart beim Provider widerrufen. Repositorywechsel ist keine Rotation und erfordert eine getrennte Projektkonfiguration und Review.

## Redaction

`GatewayLogRedactor` maskiert Bearer-Werte und benannte Credentials, einschließlich `github_token`, `github_pat`, `client_secret`, API-Keys, Passwörter und allgemeiner Token-/Secret-Felder, als `[REDACTED]`. Die Tests verwenden ausschließlich künstliche Tokenwerte und prüfen, dass diese weder im Audit noch im Redaction-Ergebnis erscheinen.