# MCP Inspector und Conformance

## Automatisierte Conformance

Der Gateway-Integrationstest `McpInitialize_ConformsToTheStreamableHttpInitializationContract`
sendet einen standardkonformen `initialize`-Request über Streamable HTTP. Er prüft die
ausgehandelte Protokollversion, Tool- und Logging-Capabilities sowie die Serveridentität.
Die Schema-Verträge der Tools werden zusätzlich in
`McpToolsList_ExposesStableRuntimeToolContracts` geprüft.

## Manueller Inspector-Smoke-Test

Voraussetzung: Node.js mit `npx`. Das Tool wird nicht im Repository installiert.

```powershell
dotnet run --project src/PatchPony.Gateway/PatchPony.Gateway.csproj
npx -y @modelcontextprotocol/inspector http://127.0.0.1:5055/mcp
```

Im Inspector verbinden, unter **Tools** die Discovery prüfen und `runtime.status`
ohne Argumente aufrufen. Erwartet wird der read-only-Modus mit einer
`correlationId`. `runtime.validate_correlation` kann mit einer gültigen ID wie
`local-test-42` und mit `invalid!` getestet werden; die ungültige Eingabe liefert
den gemeinsamen MCP-Fehlervertrag.

Der Gateway verwendet den stateless Streamable-HTTP-Transport unter `/mcp`; ein
MCP-Session-Header ist für die aktuellen read-only-Aufrufe daher nicht erforderlich.

Weiterführend: [MCP Streamable HTTP](https://modelcontextprotocol.io/specification/2025-11-25/basic/transports) und [MCP Inspector](https://modelcontextprotocol.io/docs/tools/inspector).