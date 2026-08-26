using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PatchPony.Core.Application;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

[McpServerToolType]
public sealed class RuntimeMcpTools(RuntimeStatusService status, ICorrelationContext correlations)
{
    [McpServerTool(
        Name = "runtime.status",
        Title = "PatchPony runtime status",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = false)]
    [Description("Returns the current PatchPony runtime mode and request correlation identifier without accessing projects, files, networks, or external tools.")]
    public RuntimeStatus GetStatus() => status.Get();

    [McpServerTool(
        Name = "runtime.validate_correlation",
        Title = "Validate correlation identifier",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = false, UseStructuredContent = false)]
    [Description("Validates a correlation identifier and returns the common PatchPony error contract on invalid input.")]
    public CallToolResult ValidateCorrelation([Description("Correlation identifier to validate.")] string correlationId)
    {
        var result = status.ValidateCorrelation(correlationId);
        return result.IsSuccess
            ? new CallToolResult { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(result.Value) }] }
            : GatewayErrorAdapter.ToMcpResult(result.Error, correlations);
    }}

