# Registrierte Sandbox-Runner

`ISandboxRunnerCatalog` ist ein geschlossener Katalog, der beim Worker-Start ausschließlich aus `PatchPony:Worker:SandboxRunners:Runners` aufgebaut wird.

Jeder Eintrag enthält eine stabile Runner-ID und eine Image-Referenz im Format:

```text
registry.example.test/namespace/runner@sha256:<64 lowercase hex characters>
```

Tags wie `latest`, ungebundene Image-Namen, Großbuchstaben im Digest und doppelte IDs lassen den Katalog fail-closed nicht erstellen. Ein Sandbox-Auftrag kann kein Image übergeben; spätere Command-Definitionen wählen nur eine bekannte Runner-ID. Unbekannte IDs liefern `sandbox.runner.unsupported`.
Zusätzlich ist `rootlessCompatible: true` verpflichtend. Damit bestätigt die serverseitige Runner-Registrierung, dass das Image mit dem festen nichtprivilegierten Benutzer `65532:65532`, dem schreibgeschützten Root-Dateisystem und ausschließlich dem Worktree-Mount arbeiten kann. Fehlende oder `false` gesetzte Freigaben verhindern den Worker-Start.