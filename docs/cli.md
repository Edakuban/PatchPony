# Lokale Read-only-CLI

Die CLI führt in I3 ausschließlich lokale Diagnose aus. Sie legt keine
Projekte, Jobs, Sessions oder Git-Änderungen an.

```powershell
dotnet run --project src/PatchPony.Cli/PatchPony.Cli.csproj -- project validate <manifest-file>
dotnet run --project src/PatchPony.Cli/PatchPony.Cli.csproj -- project diagnose <checkout-root> <manifest-file>
```

`project validate` prüft YAML, Schema und Manifestregeln. `project diagnose`
erstellt zusätzlich einen durch die Read-Policy begrenzten Projektbaum und
meldet dessen Grenzen. Die Ausgabe enthält keine absoluten Checkout-Pfade und
keine Inhalte aus `.env` oder anderen gesperrten Pfaden.
