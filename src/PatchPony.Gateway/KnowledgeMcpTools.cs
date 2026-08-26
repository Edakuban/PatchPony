using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

[McpServerToolType]
public sealed class KnowledgeMcpTools(KnowledgeContractCatalog knowledge, ProjectToolAuthorizationService authorization, IHttpContextAccessor context, ICorrelationContext correlations)
{
    [McpServerTool(Name = "knowledge.tree", Title = "List approved knowledge vault paths", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists only approved knowledge-vault paths for one authorized project.")]
    public CallToolResult Tree(string projectId, string? path = null, int? depth = null)
    {
        var access = Authorize(projectId, "knowledge.tree", Parameters(("path", path), ("depth", depth?.ToString())));
        return access.IsSuccess ? ToMcp(knowledge.Tree(projectId, path, depth)) : GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
    }

    [McpServerTool(Name = "knowledge.search", Title = "Search approved knowledge vault text", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches only approved knowledge-vault Markdown text for one authorized project.")]
    public CallToolResult Search(string projectId, string query)
    {
        var access = Authorize(projectId, "knowledge.search", Parameters(("query", query)));
        return access.IsSuccess ? ToMcp(knowledge.Search(projectId, query)) : GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
    }

    [McpServerTool(Name = "knowledge.read", Title = "Read approved knowledge vault Markdown", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Reads one approved Markdown file from the knowledge vault for one authorized project.")]
    public CallToolResult Read(string projectId, string path)
    {
        var access = Authorize(projectId, "knowledge.read", Parameters(("path", path)));
        return access.IsSuccess ? ToMcp(knowledge.Read(projectId, path)) : GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
    }

    [McpServerTool(Name = "knowledge.links", Title = "Inspect approved knowledge vault links", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Lists resolved links from one approved knowledge-vault Markdown file for one authorized project.")]
    public CallToolResult Links(string projectId, string path)
    {
        var access = Authorize(projectId, "knowledge.links", Parameters(("path", path)));
        return access.IsSuccess ? ToMcp(knowledge.Links(projectId, path)) : GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
    }

    private Result<ProjectToolAccessDecision> Authorize(string projectId, string tool, IReadOnlyDictionary<string, string?> parameters) => authorization.Authorize(context.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity()), projectId, new ProjectToolAccessRequest(tool, parameters));
    private CallToolResult ToMcp<T>(Result<T> result) => result.IsSuccess ? new CallToolResult { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(result.Value) }], StructuredContent = JsonSerializer.SerializeToElement(result.Value) } : GatewayErrorAdapter.ToMcpResult(result.Error, correlations);
    private static IReadOnlyDictionary<string, string?> Parameters(params (string Name, string? Value)[] values) => values.Where(item => item.Value is not null).ToDictionary(item => item.Name, item => item.Value);
}