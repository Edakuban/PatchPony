# Registrierte Sandbox-Commands

`ISandboxCommandCatalog` ordnet eine `commandId` fest einem registrierten Runner, einer festen Executable und einer unveränderlichen Argumentliste zu. Die Worker-Konfiguration liegt unter `PatchPony:Worker:SandboxCommands:Commands`.

Ein Command enthält nur serverseitige Werte:

- `id`, etwa `test.billing.unit`
- `runnerId`, die auf einen Eintrag des Runner-Katalogs zeigen muss
- absolute Executable im Container, etwa `/usr/bin/dotnet`
- höchstens 32 feste, begrenzte Argumente

Shell-Interpreter (`/bin/sh`, `/bin/bash` usw.), unbekannte Runner, nicht erlaubte IDs, Kontrollzeichen und dynamische Argumente werden beim Aufbau des Katalogs abgewiesen. Der interne Sandbox-Auftrag referenziert danach nur noch die `commandId`; er kann weder die Executable noch Argumente verändern.