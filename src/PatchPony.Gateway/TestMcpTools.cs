using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

[McpServerToolType]
public sealed class TestMcpTools(TestSessionCatalog sessions, ProjectToolAuthorizationService authorization, IHttpContextAccessor context, ICorrelationContext correlations)
{
    [McpServerTool(Name = "tests.list", Title = "List registered session tests", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists only server-registered test command IDs for one authorized session.")]
    public CallToolResult List(string projectId, string sessionId)
    {
        var access = Authorize(projectId, "tests.list", new Dictionary<string, string?> { ["sessionId"] = sessionId });
        if (!access.IsSuccess) return GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
        var result = sessions.List(projectId, sessionId);
        return result.IsSuccess ? ToMcp(result.Value!) : GatewayErrorAdapter.ToMcpResult(result.Error, correlations);
    }

    [McpServerTool(Name = "tests.run", Title = "Request a registered session test", ReadOnly = false, Idempotent = false, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Queues one server-registered test command for the private worker. Image, arguments, paths and Docker options cannot be supplied.")]
    public CallToolResult Run(string projectId, string sessionId, string commandId, CancellationToken cancellationToken)
    {
        var access = Authorize(projectId, "tests.run", new Dictionary<string, string?> { ["sessionId"] = sessionId, ["commandId"] = commandId });
        if (!access.IsSuccess) return GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
        var result = sessions.Run(projectId, sessionId, new TestRunCommand(commandId), cancellationToken);
        return result.IsSuccess ? ToMcp(result.Value!) : GatewayErrorAdapter.ToMcpResult(result.Error, correlations);
    }

    [McpServerTool(Name = "tests.result", Title = "Read a completed session test", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Reads one bounded result previously written by the private worker for an authorized session.")]
    public CallToolResult Result(string projectId, string sessionId, string executionId)
    {
        var access = Authorize(projectId, "tests.result", new Dictionary<string, string?> { ["sessionId"] = sessionId, ["executionId"] = executionId });
        if (!access.IsSuccess) return GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
        var result = sessions.Result(projectId, sessionId, executionId);
        return result.IsSuccess ? ToMcp(result.Value!) : GatewayErrorAdapter.ToMcpResult(result.Error, correlations);
    }

    private CallToolResult ToMcp<T>(T value) => new() { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(value) }], StructuredContent = JsonSerializer.SerializeToElement(value) };
    private Result<ProjectToolAccessDecision> Authorize(string projectId, string tool, IReadOnlyDictionary<string, string?> parameters) => authorization.Authorize(context.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity()), projectId, new ProjectToolAccessRequest(tool, parameters));
}