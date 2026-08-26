using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PatchPony.Core.Common;
using PatchPony.Core.Knowledge;

namespace PatchPony.Gateway;

[McpServerToolType]
public sealed class KnowledgeMcpTools(KnowledgeContractCatalog knowledge, ProjectToolAuthorizationService authorization, IHttpContextAccessor context, ICorrelationContext correlations, IKnowledgeAuditSink audit)
{
    [McpServerTool(Name = "knowledge.tree", Title = "List approved knowledge vault paths", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = false)]
    [Description("Lists only approved knowledge-vault paths for one authorized project.")]
    public CallToolResult Tree(string projectId, string? path = null, int? depth = null)
    {
        var access = Authorize(projectId, "knowledge.tree", Parameters(("path", path), ("depth", depth?.ToString())));
        return access.IsSuccess ? ToMcp(Audit(projectId, "tree", null, knowledge.Tree(projectId, path, depth), value => value.Revision, value => value.Entries.Count)) : GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
    }

    [McpServerTool(Name = "knowledge.search", Title = "Search approved knowledge vault text", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = false)]
    [Description("Searches only approved knowledge-vault Markdown text for one authorized project.")]
    public CallToolResult Search(string projectId, string query)
    {
        var access = Authorize(projectId, "knowledge.search", Parameters(("query", query)));
        return access.IsSuccess ? ToMcp(Audit(projectId, "search", null, knowledge.Search(projectId, query), value => value.Revision, value => value.Matches.Count)) : GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
    }

    [McpServerTool(Name = "knowledge.read", Title = "Read approved knowledge vault Markdown", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = false)]
    [Description("Reads one approved Markdown file from the knowledge vault for one authorized project.")]
    public CallToolResult Read(string projectId, string path)
    {
        var access = Authorize(projectId, "knowledge.read", Parameters(("path", path)));
        return access.IsSuccess ? ToMcp(Audit(projectId, "read", path, knowledge.Read(projectId, path), value => value.Revision, value => value.Lines.Count)) : GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
    }

    [McpServerTool(Name = "knowledge.links", Title = "Inspect approved knowledge vault links", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = false)]
    [Description("Lists resolved links from one approved knowledge-vault Markdown file for one authorized project.")]
    public CallToolResult Links(string projectId, string path)
    {
        var access = Authorize(projectId, "knowledge.links", Parameters(("path", path)));
        return access.IsSuccess ? ToMcp(Audit(projectId, "links", path, knowledge.Links(projectId, path), value => value.Revision, value => value.Links.Count)) : GatewayErrorAdapter.ToMcpResult(access.Error, correlations);
    }

    private Result<T> Audit<T>(string projectId, string operation, string? path, Result<T> result, Func<T, string?> revision, Func<T, int?> count)
    {
        audit.Record(new KnowledgeAuditEvent(
            DateTimeOffset.UtcNow,
            correlations.Current.Value,
            projectId,
            null,
            operation,
            result.IsSuccess ? "completed" : "rejected",
            path is null ? null : KnowledgeAuditFingerprint.ForValue(path),
            result.IsSuccess ? revision(result.Value!) : null,
            result.IsSuccess ? count(result.Value!) : null));
        return result;
    }

    private Result<ProjectToolAccessDecision> Authorize(string projectId, string tool, IReadOnlyDictionary<string, string?> parameters) => authorization.Authorize(context.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity()), projectId, new ProjectToolAccessRequest(tool, parameters));
    private CallToolResult ToMcp<T>(Result<T> result) => result.IsSuccess ? new CallToolResult { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(result.Value) }], StructuredContent = JsonSerializer.SerializeToElement(result.Value) } : GatewayErrorAdapter.ToMcpResult(result.Error, correlations);
    private static IReadOnlyDictionary<string, string?> Parameters(params (string Name, string? Value)[] values) => values.Where(item => item.Value is not null).ToDictionary(item => item.Name, item => item.Value);
}