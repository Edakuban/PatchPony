using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

[McpServerToolType]
public sealed class ConfigMcpTools(ConfigSessionCatalog sessions, ProjectToolAuthorizationService authorization, IHttpContextAccessor httpContextAccessor, ICorrelationContext correlations)
{
    [McpServerTool(Name = "config.validate", Title = "Validate a session configuration document", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Validates one existing configuration document in an authorized server-configured session. Filesystem paths are never supplied by the caller.")]
    public CallToolResult Validate(string projectId, string sessionId, string path, string format, string? schemaId = null, CancellationToken cancellationToken = default)
    {
        var decision = Authorize(projectId, "config.validate", new Dictionary<string, string?> { ["sessionId"] = sessionId, ["path"] = path, ["format"] = format, ["schemaId"] = schemaId });
        if (!decision.IsSuccess) return GatewayErrorAdapter.ToMcpResult(decision.Error, correlations);
        var result = sessions.Validate(projectId, sessionId, new ConfigValidateCommand(path, format, schemaId), cancellationToken);
        return result.IsSuccess ? ToMcp(result.Value!) : GatewayErrorAdapter.ToMcpResult(result.Error, correlations);
    }

    [McpServerTool(Name = "config.patch", Title = "Patch and validate a session configuration document", ReadOnly = false, Idempotent = false, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Applies a base64-encoded UTF-8 full-document replacement in an authorized server-configured session. The expected SHA-256 must match; invalid parser or schema results are rolled back.")]
    public CallToolResult Patch(string projectId, string sessionId, string path, string format, string expectedSourceSha256, string replacementBase64, string? schemaId = null, CancellationToken cancellationToken = default)
    {
        var decision = Authorize(projectId, "config.patch", new Dictionary<string, string?> { ["sessionId"] = sessionId, ["path"] = path, ["format"] = format, ["expectedSourceSha256"] = expectedSourceSha256, ["replacementBase64"] = replacementBase64, ["schemaId"] = schemaId });
        if (!decision.IsSuccess) return GatewayErrorAdapter.ToMcpResult(decision.Error, correlations);
        var result = sessions.Patch(projectId, sessionId, new ConfigPatchCommand(path, format, expectedSourceSha256, replacementBase64, schemaId), cancellationToken);
        return result.IsSuccess ? ToMcp(result.Value!) : GatewayErrorAdapter.ToMcpResult(result.Error, correlations);
    }

    private CallToolResult ToMcp<T>(T value) => new() { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(value) }], StructuredContent = JsonSerializer.SerializeToElement(value) };

    private Result<ProjectToolAccessDecision> Authorize(string projectId, string tool, IReadOnlyDictionary<string, string?> parameters)
    {
        var user = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        return authorization.Authorize(user, projectId, new ProjectToolAccessRequest(tool, parameters));
    }
}