using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed record SourceCitation(string ProjectId, string Path, int StartLine, int EndLine);
public sealed record SourceSearchCitation(SourceCitation Citation, string Text);
public sealed record SourceSearchMcpResult(IReadOnlyList<SourceSearchCitation> Matches, bool IsTruncated);
public sealed record SourceReadMcpResult(SourceCitation Citation, IReadOnlyList<string> Lines, bool IsTruncated);

[McpServerToolType]
public sealed class ProjectSourceMcpTools(PilotSourceCatalog sources, ProjectToolAuthorizationService authorization, IHttpContextAccessor httpContextAccessor, ICorrelationContext correlations)
{
    [McpServerTool(Name = "source.search", Title = "Search approved project sources", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches only manifest-approved text files in one authorized pilot project. Every match includes a project, path, and exact source line suitable for citation.")]
    public async Task<CallToolResult> Search([Description("Authorized pilot project identifier.")] string projectId, [Description("Literal search text (1 to 256 characters).") ] string query, CancellationToken cancellationToken)
    {
        var decision = Authorize(projectId, "source.search", new Dictionary<string, string?> { ["query"] = query });
        if (!decision.IsSuccess) return GatewayErrorAdapter.ToMcpResult(decision.Error, correlations);
        if (!sources.TryGet(projectId, out var project)) return GatewayErrorAdapter.ToMcpResult(new DomainError("project.not_found", "The requested pilot project is not available."), correlations);

        var result = await project.Search.SearchAsync(query, cancellationToken);
        if (!result.IsSuccess) return GatewayErrorAdapter.ToMcpResult(result.Error, correlations);

        var response = new SourceSearchMcpResult(result.Value!.Matches.Select(match => new SourceSearchCitation(new SourceCitation(projectId, match.RelativePath, match.LineNumber, match.LineNumber), match.LineText)).ToArray(), result.Value.IsTruncated);
        return new CallToolResult { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(response) }], StructuredContent = JsonSerializer.SerializeToElement(response) };
    }

    [McpServerTool(Name = "source.read", Title = "Read an approved project source file", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Reads one manifest-approved UTF-8 source file in an authorized pilot project. The result includes a project, path, and line range suitable for citation.")]
    public async Task<CallToolResult> Read([Description("Authorized pilot project identifier.")] string projectId, [Description("Safe relative path of a manifest-approved source file.")] string path, CancellationToken cancellationToken)
    {
        var decision = Authorize(projectId, "source.read", new Dictionary<string, string?> { ["path"] = path });
        if (!decision.IsSuccess) return GatewayErrorAdapter.ToMcpResult(decision.Error, correlations);
        if (!sources.TryGet(projectId, out var project)) return GatewayErrorAdapter.ToMcpResult(new DomainError("project.not_found", "The requested pilot project is not available."), correlations);

        var result = await project.Reader.ReadAsync(path, cancellationToken);
        if (!result.IsSuccess) return GatewayErrorAdapter.ToMcpResult(result.Error, correlations);

        var lines = result.Value!.Lines;
        var response = new SourceReadMcpResult(new SourceCitation(projectId, result.Value.RelativePath, lines.FirstOrDefault()?.Number ?? 1, lines.LastOrDefault()?.Number ?? 1), lines.Select(line => line.Text).ToArray(), result.Value.IsTruncated);
        return new CallToolResult { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(response) }], StructuredContent = JsonSerializer.SerializeToElement(response) };
    }

    private Result<ProjectToolAccessDecision> Authorize(string projectId, string tool, IReadOnlyDictionary<string, string?> parameters)
    {
        var user = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        return authorization.Authorize(user, projectId, new ProjectToolAccessRequest(tool, parameters));
    }
}