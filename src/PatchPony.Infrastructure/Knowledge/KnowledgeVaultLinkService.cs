using PatchPony.Core.Common;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;

namespace PatchPony.Infrastructure.Knowledge;

public enum KnowledgeVaultLinkKind { Wiki, Markdown }

public sealed record KnowledgeVaultLink(
    string SourcePath,
    int LineNumber,
    KnowledgeVaultLinkKind Kind,
    string Target,
    string? ResolvedPath,
    string? Fragment,
    bool IsExternal);

public sealed record KnowledgeVaultLinksResult(RepositoryRevision Revision, IReadOnlyList<KnowledgeVaultLink> Links, bool IsTruncated);
public sealed record KnowledgeVaultBacklink(string SourcePath, int LineNumber, KnowledgeVaultLinkKind Kind, string? Fragment);
public sealed record KnowledgeVaultBacklinksResult(RepositoryRevision Revision, string TargetPath, IReadOnlyList<KnowledgeVaultBacklink> Backlinks, bool IsTruncated);

public sealed class KnowledgeVaultLinkService(KnowledgeVaultService vault)
{
    private const int MaximumFiles = 200;
    private const int MaximumLinks = 500;

    public Result<KnowledgeVaultLinksResult> ResolveLinks(KnowledgeSourceCheckout checkout, string sourcePath)
    {
        var catalog = GetCatalog(checkout);
        if (!catalog.IsSuccess) return Result<KnowledgeVaultLinksResult>.Failure(catalog.Error);
        if (!catalog.Value!.Files.Contains(sourcePath)) return Result<KnowledgeVaultLinksResult>.Failure(new DomainError("vault.not_found", "The requested Markdown file was not found in the knowledge vault."));

        var read = vault.ReadMarkdown(checkout, sourcePath);
        if (!read.IsSuccess) return Result<KnowledgeVaultLinksResult>.Failure(read.Error);

        var catalogValue = catalog.Value!;
        var readValue = read.Value!;
        var links = KnowledgeMarkdownLinkParser.Parse(readValue, catalogValue.Files, MaximumLinks, out var reachedLimit);
        return Result<KnowledgeVaultLinksResult>.Success(new KnowledgeVaultLinksResult(checkout.Revision, links, catalogValue.IsTruncated || readValue.IsTruncated || reachedLimit));
    }

    public Result<KnowledgeVaultBacklinksResult> FindBacklinks(KnowledgeSourceCheckout checkout, string targetPath)
    {
        var catalog = GetCatalog(checkout);
        if (!catalog.IsSuccess) return Result<KnowledgeVaultBacklinksResult>.Failure(catalog.Error);

        var normalizedTarget = KnowledgeMarkdownLinkParser.ResolveRootTarget(targetPath, catalog.Value!.Files);
        if (normalizedTarget is null) return Result<KnowledgeVaultBacklinksResult>.Failure(new DomainError("vault.not_found", "The requested Markdown target was not found in the knowledge vault."));

        var backlinks = new List<KnowledgeVaultBacklink>();
        var isTruncated = catalog.Value.IsTruncated;
        foreach (var sourcePath in catalog.Value.Files)
        {
            var read = vault.ReadMarkdown(checkout, sourcePath);
            if (!read.IsSuccess)
            {
                isTruncated = true;
                continue;
            }

            foreach (var link in KnowledgeMarkdownLinkParser.Parse(read.Value!, catalog.Value.Files, MaximumLinks, out var reachedLimit))
            {
                isTruncated |= reachedLimit;
                if (link.ResolvedPath != normalizedTarget) continue;
                backlinks.Add(new KnowledgeVaultBacklink(link.SourcePath, link.LineNumber, link.Kind, link.Fragment));
                if (backlinks.Count == MaximumLinks)
                {
                    isTruncated = true;
                    break;
                }
            }
            isTruncated |= read.Value!.IsTruncated;
            if (backlinks.Count == MaximumLinks) break;
        }

        return Result<KnowledgeVaultBacklinksResult>.Success(new KnowledgeVaultBacklinksResult(checkout.Revision, normalizedTarget, backlinks, isTruncated));
    }

    private Result<Catalog> GetCatalog(KnowledgeSourceCheckout checkout)
    {
        var tree = vault.List(checkout);
        if (!tree.IsSuccess) return Result<Catalog>.Failure(tree.Error);
        var files = tree.Value!.Entries
            .Where(entry => entry.Kind == KnowledgeVaultTreeEntryKind.File && entry.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.RelativePath)
            .Take(MaximumFiles)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Result<Catalog>.Success(new Catalog(files, tree.Value.IsTruncated || files.Count == MaximumFiles));
    }

    private sealed record Catalog(HashSet<string> Files, bool IsTruncated);
}
