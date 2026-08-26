# MCP über Streamable HTTP

Der Gateway stellt MCP unter `POST /mcp` über den offiziellen Streamable-HTTP-Transport bereit. Der Transport läuft stateless: Pro Request wird kein MCP-Sitzungszustand im Gateway vorgehalten. Das passt zu den aktuellen read-only Fähigkeiten und ermöglicht später horizontale Skalierung ohne Session-Affinität.

Clients müssen einen Streamable-HTTP-kompatiblen `Accept`-Header senden, etwa `application/json, text/event-stream`. Die normale JSON-RPC-Request-Content-Type ist `application/json`.

Der Endpunkt enthält in diesem Schritt noch keine fachlichen Tools. Tool-Verträge und -Zuordnung folgen separat mit I4.4; Request- und Parallelitätslimits werden in I4.7 ergänzt.

Grundlage: [offizielle C#-SDK-Transportdokumentation](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/transports/transports.md).

Lokale Inspector- und automatisierte Conformance-Prüfungen sind in [mcp-inspector.md](mcp-inspector.md) beschrieben.
