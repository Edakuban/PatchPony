using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using PatchPony.Core.Common;

namespace PatchPony.Gateway;

public sealed record SourceCitation(string ProjectId, string Path, int StartLine, int EndLine, string? Url = null);
public sealed record SourceSearchCitation(SourceCitation Citation, string Text);
public sealed record SourceSearchMcpResult(IReadOnlyList<SourceSearchCitation> Matches, bool IsTruncated);
public sealed record SourceReadMcpResult(SourceCitation Citation, IReadOnlyList<string> Lines, bool IsTruncated);

[McpServerToolType]
public sealed class ProjectSourceMcpTools(PilotSourceCatalog sources, ProjectToolAuthorizationService authorization, IHttpContextAccessor httpContextAccessor, ICorrelationContext correlations)
{
    private const int DefaultMcpReadLines = 120;
    private const int MaximumMcpReadLines = 500;
    [McpServerTool(Name = "source.search", Title = "Search approved project sources", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = false)]
    [Description("Searches only manifest-approved text files in one authorized pilot project. Every match includes a project, path, and exact source line suitable for citation.")]
    public async Task<CallToolResult> Search([Description("Authorized pilot project identifier.")] string projectId, [Description("Literal search text (1 to 256 characters).") ] string query, CancellationToken cancellationToken)
    {
        var decision = Authorize(projectId, "source.search", new Dictionary<string, string?> { ["query"] = query });
        if (!decision.IsSuccess) return GatewayErrorAdapter.ToMcpResult(decision.Error, correlations);
        if (!sources.TryGet(projectId, out var project)) return GatewayErrorAdapter.ToMcpResult(new DomainError("project.not_found", "The requested pilot project is not available."), correlations);

        var result = await project.Search.SearchAsync(query, cancellationToken);
        if (!result.IsSuccess) return GatewayErrorAdapter.ToMcpResult(result.Error, correlations);

        var response = new SourceSearchMcpResult(result.Value!.Matches.Select(match => new SourceSearchCitation(new SourceCitation(projectId, match.RelativePath, match.LineNumber, match.LineNumber, BuildCitationUrl(project, match.RelativePath, match.LineNumber, match.LineNumber)), match.LineText)).ToArray(), result.Value.IsTruncated);
        return new CallToolResult { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(response) }], StructuredContent = JsonSerializer.SerializeToElement(response) };
    }

    [McpServerTool(Name = "source.read", Title = "Read an approved project source file", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = false)]
    [Description("Reads one manifest-approved UTF-8 source file in an authorized pilot project. The result includes a project, path, and line range suitable for citation.")]
    public async Task<CallToolResult> Read([Description("Authorized pilot project identifier.")] string projectId, [Description("Safe relative path of a manifest-approved source file.")] string path, [Description("Optional maximum number of lines to return (1 to 500). Defaults to 120.")] int? maxLines = null, CancellationToken cancellationToken = default)
    {
        if (maxLines is < 1 or > MaximumMcpReadLines)
        {
            return GatewayErrorAdapter.ToMcpResult(new DomainError("source.invalid_max_lines", "The maximum line count must be between 1 and 500."), correlations);
        }

        var decision = Authorize(projectId, "source.read", new Dictionary<string, string?> { ["path"] = path, ["maxLines"] = maxLines?.ToString() });
        if (!decision.IsSuccess) return GatewayErrorAdapter.ToMcpResult(decision.Error, correlations);
        if (!sources.TryGet(projectId, out var project)) return GatewayErrorAdapter.ToMcpResult(new DomainError("project.not_found", "The requested pilot project is not available."), correlations);

        var result = await project.Reader.ReadAsync(path, cancellationToken);
        if (!result.IsSuccess) return GatewayErrorAdapter.ToMcpResult(result.Error, correlations);

        var source = result.Value!;
        var requestedLineCount = maxLines ?? DefaultMcpReadLines;
        var lines = source.Lines.Take(requestedLineCount).ToArray();
        var response = new SourceReadMcpResult(
            new SourceCitation(projectId, source.RelativePath, lines.FirstOrDefault()?.Number ?? 1, lines.LastOrDefault()?.Number ?? 1, BuildCitationUrl(project, source.RelativePath, lines.FirstOrDefault()?.Number ?? 1, lines.LastOrDefault()?.Number ?? 1)),
            lines.Select(line => line.Text).ToArray(),
            source.IsTruncated || source.Lines.Count > requestedLineCount);
        return new CallToolResult { Content = [new TextContentBlock { Text = JsonSerializer.Serialize(response) }], StructuredContent = JsonSerializer.SerializeToElement(response) };
    }

    private static string? BuildCitationUrl(PilotSourceProject project, string path, int startLine, int endLine)
    {
        var template = project.CitationUrlTemplate;
        if (string.IsNullOrWhiteSpace(template))
        {
            var baseUri = project.RemoteUri.AbsoluteUri.TrimEnd('/');
            if (baseUri.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) baseUri = baseUri[..^4];
            template = project.RemoteUri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
                ? baseUri + "/blob/{branch}/{path}#L{startLine}"
                : project.RemoteUri.Host.Contains("gitlab", StringComparison.OrdinalIgnoreCase)
                    ? baseUri + "/-/blob/{branch}/{path}#L{startLine}"
                    : string.Empty;
        }
        if (string.IsNullOrWhiteSpace(template)) return null;
        var encodedPath = string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
        var url = template.Replace("{path}", encodedPath, StringComparison.Ordinal).Replace("{branch}", Uri.EscapeDataString(project.DefaultBranch), StringComparison.Ordinal).Replace("{startLine}", startLine.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal).Replace("{endLine}", endLine.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri.AbsoluteUri : null;
    }
    private Result<ProjectToolAccessDecision> Authorize(string projectId, string tool, IReadOnlyDictionary<string, string?> parameters)
    {
        var user = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        return authorization.Authorize(user, projectId, new ProjectToolAccessRequest(tool, parameters));
    }
}