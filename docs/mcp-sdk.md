# MCP SDK

PatchPony verwendet das offizielle C# SDK `ModelContextProtocol.AspNetCore` in Version `2.2.0`. Das Paket enthält das MCP-Kern-/DI-SDK und die ASP.NET-Core-Transportintegration und ist zentral in `Directory.Packages.props` gepinnt.

Der Gateway registriert mit `AddMcpServer()` die MCP-Serverdienste. Der stateless Streamable-HTTP-Transport ist unter `/mcp` konfiguriert; die eigentlichen Tool-Verträge folgen erst in I4.4.

Die SDK-Wahl folgt der offiziellen Paketempfehlung für HTTP-basierte MCP-Server: [MCP C# SDK – Getting started](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/getting-started.md) und [NuGet: ModelContextProtocol.AspNetCore](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore/2.2.0).
