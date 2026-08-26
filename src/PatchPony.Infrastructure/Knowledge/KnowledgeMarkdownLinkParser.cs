using System.Text.RegularExpressions;

namespace PatchPony.Infrastructure.Knowledge;

/// <summary>
/// Parses Markdown and Obsidian wiki links and resolves internal targets only against a supplied vault file catalog.
/// It never touches the filesystem and rejects paths that cannot be represented canonically inside the vault.
/// </summary>
public static class KnowledgeMarkdownLinkParser
{
    private static readonly Regex WikiLinkPattern = new("!?\\[\\[(?<target>[^\\]\\r\\n]{1,512})\\]\\]", RegexOptions.CultureInvariant);
    private static readonly Regex MarkdownLinkPattern = new("(?<!!)\\[[^\\]\\r\\n]{0,512}\\]\\((?<target>[^)\\r\\n]{1,2048})\\)", RegexOptions.CultureInvariant);

    public static IReadOnlyList<KnowledgeVaultLink> Parse(KnowledgeVaultReadResult read, IReadOnlySet<string> files, int maximumLinks, out bool isTruncated)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(files);
        if (maximumLinks < 1) throw new ArgumentOutOfRangeException(nameof(maximumLinks));

        var sourcePath = NormalizeVaultPath(read.RelativePath);
        if (sourcePath is null) throw new ArgumentException("The source path must be a canonical vault-relative path.", nameof(read));

        var catalog = files
            .Select(NormalizeVaultPath)
            .Where(path => path is not null)
            .Select(path => path!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var links = new List<KnowledgeVaultLink>();
        isTruncated = false;

        foreach (var line in read.Lines)
        {
            foreach (Match match in WikiLinkPattern.Matches(line.Text))
            {
                Add(links, sourcePath, line.Number, KnowledgeVaultLinkKind.Wiki, match.Groups["target"].Value, catalog);
                if (links.Count >= maximumLinks) return Truncate(links, out isTruncated);
            }
            foreach (Match match in MarkdownLinkPattern.Matches(line.Text))
            {
                Add(links, sourcePath, line.Number, KnowledgeVaultLinkKind.Markdown, match.Groups["target"].Value, catalog);
                if (links.Count >= maximumLinks) return Truncate(links, out isTruncated);
            }
        }

        return links;
    }

    public static string? ResolveRootTarget(string targetPath, IReadOnlySet<string> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var catalog = files.Select(NormalizeVaultPath).Where(path => path is not null).Select(path => path!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return FindMarkdownCandidate(NormalizeRelative(string.Empty, targetPath), catalog);
    }

    private static IReadOnlyList<KnowledgeVaultLink> Truncate(List<KnowledgeVaultLink> links, out bool isTruncated)
    {
        isTruncated = true;
        return links;
    }

    private static void Add(List<KnowledgeVaultLink> links, string sourcePath, int lineNumber, KnowledgeVaultLinkKind kind, string rawTarget, IReadOnlySet<string> files)
    {
        var target = rawTarget.Trim();
        if (kind == KnowledgeVaultLinkKind.Wiki)
        {
            var aliasSeparator = target.IndexOf('|');
            if (aliasSeparator >= 0) target = target[..aliasSeparator].Trim();
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

        links.Add(new KnowledgeVaultLink(sourcePath, lineNumber, kind, target, ResolveTarget(sourcePath, path, kind, files), fragment, IsExternal: false));
    }

    private static string? ResolveTarget(string sourcePath, string path, KnowledgeVaultLinkKind kind, IReadOnlySet<string> files)
    {
        if (path.Length == 0) return sourcePath;
        var sourceDirectory = Path.GetDirectoryName(sourcePath.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
        var candidate = NormalizeRelative(sourceDirectory, path);
        var resolved = FindMarkdownCandidate(candidate, files);
        if (resolved is not null || kind == KnowledgeVaultLinkKind.Markdown || path.Contains('/')) return resolved;

        var matchingNames = files.Where(file => string.Equals(Path.GetFileNameWithoutExtension(file), path, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        return matchingNames.Length == 1 ? matchingNames[0] : null;
    }

    private static string? NormalizeVaultPath(string path)
    {
        var normalized = NormalizeRelative(string.Empty, path);
        return normalized is not null && string.Equals(normalized, path, StringComparison.Ordinal) ? normalized : null;
    }

    private static string? NormalizeRelative(string baseDirectory, string target)
    {
        if (string.IsNullOrWhiteSpace(target) || Path.IsPathRooted(target) || target.Contains('\\') || target.Contains('?') || target.Contains(':') || target.IndexOfAny(['\0', '\r', '\n']) >= 0) return null;
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
        return segments.Count == 0 ? null : string.Join('/', segments);
    }

    private static string? FindMarkdownCandidate(string? candidate, IReadOnlySet<string> files)
    {
        if (candidate is null) return null;
        var exact = files.FirstOrDefault(file => string.Equals(file, candidate, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;
        return candidate.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? null : files.FirstOrDefault(file => string.Equals(file, $"{candidate}.md", StringComparison.OrdinalIgnoreCase));
    }

    private static (string Path, string? Fragment) SplitFragment(string target)
    {
        var index = target.IndexOf('#');
        return index < 0 ? (target, null) : (target[..index].Trim(), target[(index + 1)..].Trim());
    }

    private static bool IsExternal(string target) => Uri.TryCreate(target, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "mailto";
}