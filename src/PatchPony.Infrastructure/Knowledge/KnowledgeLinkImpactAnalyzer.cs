using System.Text.RegularExpressions;
using PatchPony.Core.Common;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;

namespace PatchPony.Infrastructure.Knowledge;

public sealed record KnowledgeVaultPage(string Path, string Content);
public sealed record KnowledgeLinkReference(string SourcePath, int Line, KnowledgeVaultLinkKind Kind, string? TargetPath, string? Fragment);
public sealed record KnowledgeRenameImpact(string SourcePath, string TargetPath, IReadOnlyList<KnowledgeLinkReference> BacklinksRequiringUpdate);
public sealed record KnowledgeChangeImpact(string Path, IReadOnlyList<KnowledgeLinkReference> AddedLinks, IReadOnlyList<KnowledgeLinkReference> RemovedLinks, IReadOnlyList<KnowledgeLinkReference> IncomingBacklinks, IReadOnlyList<KnowledgeLinkReference> BrokenFragmentBacklinks);

/// <summary>Pure, bounded link analysis over caller-supplied page text. It neither reads nor writes a vault.</summary>
public sealed class KnowledgeLinkImpactAnalyzer
{
    public const int MaximumPages = 200;
    private static readonly KnowledgeVaultContentPolicy ContentPolicy = new();
    private static readonly Regex Heading = new("^#{1,6}\\s+(?<text>.+?)\\s*#*\\s*$", RegexOptions.CultureInvariant);
    private static readonly RepositoryRevision AnalysisRevision = RepositoryRevision.Create("0123456789abcdef0123456789abcdef01234567").Value!;

    public Result<KnowledgeRenameImpact> AnalyzeRename(IReadOnlyList<KnowledgeVaultPage> pages, string sourcePath, string targetPath)
    {
        var prepared = Prepare(pages);
        if (!prepared.IsSuccess) return Result<KnowledgeRenameImpact>.Failure(prepared.Error);
        if (!prepared.Value!.Pages.TryGetValue(sourcePath, out _)) return Result<KnowledgeRenameImpact>.Failure(new DomainError("knowledge.impact.source_missing", "The source page does not exist in the analysis catalog."));
        if (prepared.Value.Pages.ContainsKey(targetPath)) return Result<KnowledgeRenameImpact>.Failure(new DomainError("knowledge.impact.target_exists", "The rename target already exists in the analysis catalog."));
        var targetAccess = ValidatePagePath(targetPath);
        if (!targetAccess.IsSuccess) return Result<KnowledgeRenameImpact>.Failure(targetAccess.Error);

        var backlinks = ExtractAll(prepared.Value.Pages, prepared.Value.Files)
            .Where(link => string.Equals(link.ResolvedPath, sourcePath, StringComparison.OrdinalIgnoreCase))
            .Select(ToReference)
            .OrderBy(link => link.SourcePath, StringComparer.Ordinal)
            .ThenBy(link => link.Line)
            .ToArray();
        return Result<KnowledgeRenameImpact>.Success(new KnowledgeRenameImpact(sourcePath, targetPath, backlinks));
    }

    public Result<KnowledgeChangeImpact> AnalyzeChange(IReadOnlyList<KnowledgeVaultPage> pages, string path, string proposedContent)
    {
        var prepared = Prepare(pages);
        if (!prepared.IsSuccess) return Result<KnowledgeChangeImpact>.Failure(prepared.Error);
        if (!prepared.Value!.Pages.TryGetValue(path, out var current)) return Result<KnowledgeChangeImpact>.Failure(new DomainError("knowledge.impact.source_missing", "The changed page does not exist in the analysis catalog."));
        if (proposedContent is null || proposedContent.Length > KnowledgePatchService.MaximumDocumentBytes) return Result<KnowledgeChangeImpact>.Failure(new DomainError("knowledge.impact.invalid_content", "The proposed Markdown content is invalid or too large."));

        var currentLinks = Extract(current, prepared.Value.Files).ToArray();
        var proposedLinks = Extract(new KnowledgeVaultPage(path, proposedContent), prepared.Value.Files).ToArray();
        var added = proposedLinks.Where(link => !currentLinks.Any(existing => SameLink(existing, link))).Select(ToReference).ToArray();
        var removed = currentLinks.Where(link => !proposedLinks.Any(proposed => SameLink(proposed, link))).Select(ToReference).ToArray();
        var incoming = ExtractAll(prepared.Value.Pages, prepared.Value.Files).Where(link => string.Equals(link.ResolvedPath, path, StringComparison.OrdinalIgnoreCase)).Select(ToReference).ToArray();
        var removedFragments = HeadingFragments(current.Content).Except(HeadingFragments(proposedContent), StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var broken = incoming.Where(link => link.Fragment is not null && removedFragments.Contains(NormalizeFragment(link.Fragment))).ToArray();
        return Result<KnowledgeChangeImpact>.Success(new KnowledgeChangeImpact(path, added, removed, incoming, broken));
    }

    private static Result<Catalog> Prepare(IReadOnlyList<KnowledgeVaultPage> pages)
    {
        if (pages is null || pages.Count is 0 or > MaximumPages) return Result<Catalog>.Failure(new DomainError("knowledge.impact.catalog_invalid", "The analysis catalog must contain between one and 200 pages."));
        var mapped = new Dictionary<string, KnowledgeVaultPage>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in pages)
        {
            if (page is null || page.Content is null || page.Content.Length > KnowledgePatchService.MaximumDocumentBytes) return Result<Catalog>.Failure(new DomainError("knowledge.impact.catalog_invalid", "The analysis catalog contains invalid page content."));
            var path = ValidatePagePath(page.Path);
            if (!path.IsSuccess || !mapped.TryAdd(page.Path, page)) return Result<Catalog>.Failure(path.IsSuccess ? new DomainError("knowledge.impact.catalog_invalid", "The analysis catalog contains duplicate paths.") : path.Error);
        }
        return Result<Catalog>.Success(new Catalog(mapped, mapped.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase)));
    }

    private static Result ValidatePagePath(string path)
    {
        var content = ContentPolicy.ValidateReadablePath(path);
        return !content.IsSuccess ? content : path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? Result.Success()
            : Result.Failure(new DomainError("knowledge.impact.invalid_path", "Only safe Markdown pages can be analyzed."));
    }

    private static IEnumerable<KnowledgeVaultLink> ExtractAll(IReadOnlyDictionary<string, KnowledgeVaultPage> pages, IReadOnlySet<string> files) => pages.Values.SelectMany(page => Extract(page, files));
    private static IReadOnlyList<KnowledgeVaultLink> Extract(KnowledgeVaultPage page, IReadOnlySet<string> files) => KnowledgeMarkdownLinkParser.Parse(new KnowledgeVaultReadResult(AnalysisRevision, page.Path, Normalize(page.Content).Split('\n').Select((line, index) => new KnowledgeVaultLine(index + 1, line)).ToArray(), false), files, 500, out _);
    private static KnowledgeLinkReference ToReference(KnowledgeVaultLink link) => new(link.SourcePath, link.LineNumber, link.Kind, link.ResolvedPath, link.Fragment);
    private static bool SameLink(KnowledgeVaultLink left, KnowledgeVaultLink right) => left.Kind == right.Kind && left.IsExternal == right.IsExternal && string.Equals(left.ResolvedPath, right.ResolvedPath, StringComparison.OrdinalIgnoreCase) && string.Equals(left.Fragment, right.Fragment, StringComparison.OrdinalIgnoreCase) && string.Equals(left.Target, right.Target, StringComparison.Ordinal);
    private static IEnumerable<string> HeadingFragments(string content) => Normalize(content).Split('\n').Select(line => Heading.Match(line)).Where(match => match.Success).Select(match => NormalizeFragment(match.Groups["text"].Value)).Where(fragment => fragment.Length > 0);
    private static string NormalizeFragment(string value)
    {
        var fragment = string.Concat(value.Trim().ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : character is ' ' or '-' ? '-' : '\0').Where(character => character != '\0'));
        while (fragment.Contains("--", StringComparison.Ordinal)) fragment = fragment.Replace("--", "-", StringComparison.Ordinal);
        return fragment.Trim('-');
    }
    private static string Normalize(string content) => content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    private sealed record Catalog(IReadOnlyDictionary<string, KnowledgeVaultPage> Pages, IReadOnlySet<string> Files);
}