using System.Text.RegularExpressions;
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
    private static readonly Regex WikiLinkPattern = new("!?\\[\\[(?<target>[^\\]\\r\\n]{1,512})\\]\\]", RegexOptions.CultureInvariant);
    private static readonly Regex MarkdownLinkPattern = new("(?<!!)\\[[^\\]\\r\\n]{0,512}\\]\\((?<target>[^)\\r\\n]{1,2048})\\)", RegexOptions.CultureInvariant);

    public Result<KnowledgeVaultLinksResult> ResolveLinks(KnowledgeSourceCheckout checkout, string sourcePath)
    {
        var catalog = GetCatalog(checkout);
        if (!catalog.IsSuccess)
        {
            return Result<KnowledgeVaultLinksResult>.Failure(catalog.Error);
        }

        if (!catalog.Value!.Files.Contains(sourcePath))
        {
            return Result<KnowledgeVaultLinksResult>.Failure(new DomainError("vault.not_found", "The requested Markdown file was not found in the knowledge vault."));
        }

        var read = vault.ReadMarkdown(checkout, sourcePath);
        if (!read.IsSuccess)
        {
            return Result<KnowledgeVaultLinksResult>.Failure(read.Error);
        }

        var catalogValue = catalog.Value!;
        var readValue = read.Value!;
        var links = ExtractLinks(readValue, catalogValue.Files, out var reachedLimit);
        return Result<KnowledgeVaultLinksResult>.Success(new KnowledgeVaultLinksResult(checkout.Revision, links, catalogValue.IsTruncated || readValue.IsTruncated || reachedLimit));
    }

    public Result<KnowledgeVaultBacklinksResult> FindBacklinks(KnowledgeSourceCheckout checkout, string targetPath)
    {
        var catalog = GetCatalog(checkout);
        if (!catalog.IsSuccess)
        {
            return Result<KnowledgeVaultBacklinksResult>.Failure(catalog.Error);
        }

        var normalizedTarget = NormalizeRootTarget(targetPath, catalog.Value!.Files);
        if (normalizedTarget is null)
        {
            return Result<KnowledgeVaultBacklinksResult>.Failure(new DomainError("vault.not_found", "The requested Markdown target was not found in the knowledge vault."));
        }

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

            foreach (var link in ExtractLinks(read.Value!, catalog.Value.Files, out var reachedLimit))
            {
                isTruncated |= reachedLimit;
                if (link.ResolvedPath != normalizedTarget)
                {
                    continue;
                }

                backlinks.Add(new KnowledgeVaultBacklink(link.SourcePath, link.LineNumber, link.Kind, link.Fragment));
                if (backlinks.Count == MaximumLinks)
                {
                    isTruncated = true;
                    break;
                }
                isTruncated |= reachedLimit;
            }

            isTruncated |= read.Value!.IsTruncated;
            if (backlinks.Count == MaximumLinks)
            {
                break;
            }
        }

        return Result<KnowledgeVaultBacklinksResult>.Success(new KnowledgeVaultBacklinksResult(checkout.Revision, normalizedTarget, backlinks, isTruncated));
    }

    private Result<Catalog> GetCatalog(KnowledgeSourceCheckout checkout)
    {
        var tree = vault.List(checkout);
        if (!tree.IsSuccess)
        {
            return Result<Catalog>.Failure(tree.Error);
        }

        var files = tree.Value!.Entries
            .Where(entry => entry.Kind == KnowledgeVaultTreeEntryKind.File && entry.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.RelativePath)
            .Take(MaximumFiles)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Result<Catalog>.Success(new Catalog(files, tree.Value.IsTruncated || files.Count == MaximumFiles));
    }

    private static IReadOnlyList<KnowledgeVaultLink> ExtractLinks(KnowledgeVaultReadResult read, IReadOnlySet<string> files, out bool reachedLimit)
    {
        var links = new List<KnowledgeVaultLink>();
        reachedLimit = false;
        foreach (var line in read.Lines)
        {
            foreach (Match match in WikiLinkPattern.Matches(line.Text))
            {
                AddLink(links, read.RelativePath, line.Number, KnowledgeVaultLinkKind.Wiki, match.Groups["target"].Value, files);
                if (links.Count == MaximumLinks)
                {
                    reachedLimit = true;
                    return links;
                }
            }

            foreach (Match match in MarkdownLinkPattern.Matches(line.Text))
            {
                AddLink(links, read.RelativePath, line.Number, KnowledgeVaultLinkKind.Markdown, match.Groups["target"].Value, files);
                if (links.Count == MaximumLinks)
                {
                    reachedLimit = true;
                    return links;
                }
            }
        }

        return links;
    }

    private static void AddLink(List<KnowledgeVaultLink> links, string sourcePath, int lineNumber, KnowledgeVaultLinkKind kind, string rawTarget, IReadOnlySet<string> files)
    {
        var target = rawTarget.Trim();
        if (kind == KnowledgeVaultLinkKind.Wiki)
        {
            var aliasSeparator = target.IndexOf('|');
            if (aliasSeparator >= 0)
            {
                target = target[..aliasSeparator].Trim();
            }
        }
        else if (target.StartsWith('<') && target.EndsWith('>'))
        {
            target = target[1..^1].Trim();
        }

        var (path, fragment) = SplitFragment(target);
        if (IsExternal(path))
        {
            links.Add(new KnowledgeVaultLink(sourcePath, lineNumber, kind, target, null, fragment, IsExternal: true));
            return;
        }

        var resolved = ResolveTarget(sourcePath, path, kind, files);
        links.Add(new KnowledgeVaultLink(sourcePath, lineNumber, kind, target, resolved, fragment, IsExternal: false));
    }

    private static string? ResolveTarget(string sourcePath, string path, KnowledgeVaultLinkKind kind, IReadOnlySet<string> files)
    {
        if (string.IsNullOrEmpty(path))
        {
            return sourcePath;
        }

        var sourceDirectory = Path.GetDirectoryName(sourcePath.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
        var relativeCandidate = NormalizeRelative(sourceDirectory, path);
        var resolved = FindMarkdownCandidate(relativeCandidate, files);
        if (resolved is not null || kind == KnowledgeVaultLinkKind.Markdown || path.Contains('/'))
        {
            return resolved;
        }

        var matchingNames = files.Where(file => string.Equals(Path.GetFileNameWithoutExtension(file), path, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        return matchingNames.Length == 1 ? matchingNames[0] : null;
    }

    private static string? NormalizeRootTarget(string targetPath, IReadOnlySet<string> files) => FindMarkdownCandidate(NormalizeRelative(string.Empty, targetPath), files);

    private static string? NormalizeRelative(string baseDirectory, string target)
    {
        if (string.IsNullOrWhiteSpace(target) || Path.IsPathRooted(target) || target.Contains('\\') || target.Contains('?'))
        {
            return null;
        }

        try
        {
            var segments = new List<string>();
            foreach (var segment in baseDirectory.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries).Concat(target.Split('/', StringSplitOptions.RemoveEmptyEntries)))
            {
                if (segment is "." or "") continue;
                if (segment == "..")
                {
                    if (segments.Count == 0) return null;
                    segments.RemoveAt(segments.Count - 1);
                    continue;
                }
                segments.Add(segment);
            }
            return string.Join('/', segments);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? FindMarkdownCandidate(string? candidate, IReadOnlySet<string> files)
    {
        if (candidate is null) return null;
        var exact = files.FirstOrDefault(file => string.Equals(file, candidate, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;
        return candidate.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? null
            : files.FirstOrDefault(file => string.Equals(file, $"{candidate}.md", StringComparison.OrdinalIgnoreCase));
    }

    private static (string Path, string? Fragment) SplitFragment(string target)
    {
        var index = target.IndexOf('#');
        return index < 0 ? (target, null) : (target[..index].Trim(), target[(index + 1)..].Trim());
    }

    private static bool IsExternal(string target) => Uri.TryCreate(target, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "mailto";
    private sealed record Catalog(HashSet<string> Files, bool IsTruncated);
}
