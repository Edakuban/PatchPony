using System.Text;
using PatchPony.Core.Common;
using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Paths;

namespace PatchPony.Infrastructure.Knowledge;

public enum KnowledgeVaultTreeEntryKind { File, Directory }

public sealed record KnowledgeVaultTreeEntry(string RelativePath, KnowledgeVaultTreeEntryKind Kind, long? SizeBytes);
public sealed record KnowledgeVaultTreeResult(RepositoryRevision Revision, IReadOnlyList<KnowledgeVaultTreeEntry> Entries, bool IsTruncated, bool HasOversizedEntries);
public sealed record KnowledgeVaultLine(int Number, string Text);
public sealed record KnowledgeVaultReadResult(RepositoryRevision Revision, string RelativePath, IReadOnlyList<KnowledgeVaultLine> Lines, bool IsTruncated);
public sealed record KnowledgeVaultSearchMatch(string RelativePath, int LineNumber, string LineText);
public sealed record KnowledgeVaultSearchResult(RepositoryRevision Revision, IReadOnlyList<KnowledgeVaultSearchMatch> Matches, bool IsTruncated);

public sealed class KnowledgeVaultService
{
    private const int MaximumDepth = 5;
    private const int MaximumTreeEntries = 500;
    private const long MaximumTreeFileBytes = 1_048_576;
    private const int MaximumReadFileBytes = 128 * 1024;
    private const int MaximumReadLines = 500;
    private const int MaximumQueryCharacters = 256;
    private const int MaximumCandidateFiles = 200;
    private const int MaximumResults = 100;
    private const int MaximumMatchesPerFile = 20;
    private static readonly UTF8Encoding Utf8WithoutReplacement = new(false, true);

    private readonly string storageRoot;

    public KnowledgeVaultService(string storageRoot)
    {
        if (!Path.IsPathFullyQualified(storageRoot))
        {
            throw new ArgumentException("The knowledge checkout storage root must be an absolute server-side path.", nameof(storageRoot));
        }

        this.storageRoot = Path.GetFullPath(storageRoot);
    }

    public Result<KnowledgeVaultTreeResult> List(KnowledgeSourceCheckout checkout)
    {
        var context = CreateContext(checkout);
        if (!context.IsSuccess)
        {
            return Result<KnowledgeVaultTreeResult>.Failure(context.Error);
        }

        var entries = new List<KnowledgeVaultTreeEntry>();
        var state = new TreeState();
        var traversal = Traverse(context.Value!.Root, context.Value.Resolver, string.Empty, 0, entries, state);
        return traversal.IsSuccess
            ? Result<KnowledgeVaultTreeResult>.Success(new KnowledgeVaultTreeResult(checkout.Revision, entries, state.IsTruncated, state.HasOversizedEntries))
            : Result<KnowledgeVaultTreeResult>.Failure(traversal.Error);
    }

    public Result<KnowledgeVaultReadResult> ReadMarkdown(KnowledgeSourceCheckout checkout, string relativePath)
    {
        var context = CreateContext(checkout);
        if (!context.IsSuccess)
        {
            return Result<KnowledgeVaultReadResult>.Failure(context.Error);
        }

        if (!IsMarkdown(relativePath))
        {
            return Result<KnowledgeVaultReadResult>.Failure(new DomainError("vault.not_markdown", "Only Markdown files can be read from the knowledge vault."));
        }

        var resolved = context.Value!.Resolver.Resolve(relativePath);
        if (!resolved.IsSuccess)
        {
            return Result<KnowledgeVaultReadResult>.Failure(resolved.Error);
        }

        if (!File.Exists(resolved.Value!.FullPath))
        {
            return Result<KnowledgeVaultReadResult>.Failure(new DomainError("vault.not_found", "The requested Markdown file was not found."));
        }

        try
        {
            if (IsReparsePoint(resolved.Value.FullPath))
            {
                return Result<KnowledgeVaultReadResult>.Failure(new DomainError("vault.unavailable", "The requested Markdown file is unavailable."));
            }

            var bytes = File.ReadAllBytes(resolved.Value.FullPath);
            if (bytes.Length > MaximumReadFileBytes)
            {
                return Result<KnowledgeVaultReadResult>.Failure(new DomainError("vault.too_large", "The requested Markdown file exceeds the configured byte limit."));
            }

            if (bytes.Contains((byte)0))
            {
                return Result<KnowledgeVaultReadResult>.Failure(new DomainError("vault.binary", "The requested Markdown file is binary and cannot be returned as text."));
            }

            var allLines = DecodeLines(bytes);
            var lines = allLines.Take(MaximumReadLines).Select((line, index) => new KnowledgeVaultLine(index + 1, line)).ToArray();
            return Result<KnowledgeVaultReadResult>.Success(new KnowledgeVaultReadResult(checkout.Revision, resolved.Value.RelativePath, lines, allLines.Length > MaximumReadLines));
        }
        catch (DecoderFallbackException)
        {
            return Result<KnowledgeVaultReadResult>.Failure(new DomainError("vault.invalid_encoding", "The requested Markdown file is not valid UTF-8 text."));
        }
        catch (IOException)
        {
            return Result<KnowledgeVaultReadResult>.Failure(new DomainError("vault.unavailable", "The requested Markdown file is unavailable."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<KnowledgeVaultReadResult>.Failure(new DomainError("vault.unavailable", "The requested Markdown file is unavailable."));
        }
    }

    public Result<KnowledgeVaultSearchResult> Search(KnowledgeSourceCheckout checkout, string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > MaximumQueryCharacters)
        {
            return Result<KnowledgeVaultSearchResult>.Failure(new DomainError("vault.invalid_query", "The search query must contain between 1 and 256 characters."));
        }

        var context = CreateContext(checkout);
        if (!context.IsSuccess)
        {
            return Result<KnowledgeVaultSearchResult>.Failure(context.Error);
        }

        var candidates = new List<string>();
        var state = new CandidateState();
        var collect = CollectMarkdown(context.Value!.Root, context.Value.Resolver, string.Empty, candidates, state);
        if (!collect.IsSuccess)
        {
            return Result<KnowledgeVaultSearchResult>.Failure(collect.Error);
        }

        var matches = new List<KnowledgeVaultSearchMatch>();
        foreach (var candidate in candidates)
        {
            var read = ReadMarkdown(checkout, candidate);
            if (!read.IsSuccess)
            {
                state.IsTruncated = true;
                continue;
            }

            var matchesInFile = 0;
            foreach (var line in read.Value!.Lines)
            {
                if (!line.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matches.Add(new KnowledgeVaultSearchMatch(read.Value.RelativePath, line.Number, line.Text));
                matchesInFile++;
                if (matchesInFile == MaximumMatchesPerFile || matches.Count == MaximumResults)
                {
                    state.IsTruncated = true;
                    break;
                }
            }

            if (read.Value.IsTruncated || state.IsTruncated && matches.Count == MaximumResults)
            {
                state.IsTruncated = true;
            }

            if (matches.Count == MaximumResults)
            {
                break;
            }
        }

        return Result<KnowledgeVaultSearchResult>.Success(new KnowledgeVaultSearchResult(checkout.Revision, matches, state.IsTruncated));
    }

    private Result<VaultContext> CreateContext(KnowledgeSourceCheckout checkout)
    {
        if (checkout.SourceId.Value == Guid.Empty)
        {
            return Result<VaultContext>.Failure(DomainError.Validation("A knowledge source checkout is required."));
        }

        var root = Path.Combine(storageRoot, checkout.SourceId.Value.ToString("N"));
        return Directory.Exists(root)
            ? Result<VaultContext>.Success(new VaultContext(root, new ProjectPathResolver(root)))
            : Result<VaultContext>.Failure(new DomainError("vault.unavailable", "The knowledge vault checkout is unavailable."));
    }

    private static Result Traverse(string directory, ProjectPathResolver resolver, string relativeDirectory, int depth, List<KnowledgeVaultTreeEntry> entries, TreeState state)
    {
        try
        {
            foreach (var fullPath in Directory.EnumerateFileSystemEntries(directory))
            {
                if (state.ExaminedEntries++ == MaximumTreeEntries)
                {
                    state.IsTruncated = true;
                    return Result.Success();
                }

                if (IsReparsePoint(fullPath))
                {
                    continue;
                }

                var relativePath = string.IsNullOrEmpty(relativeDirectory) ? Path.GetFileName(fullPath) : $"{relativeDirectory}/{Path.GetFileName(fullPath)}";
                var resolved = resolver.Resolve(relativePath);
                if (!resolved.IsSuccess)
                {
                    continue;
                }

                if (Directory.Exists(fullPath))
                {
                    entries.Add(new KnowledgeVaultTreeEntry(resolved.Value!.RelativePath, KnowledgeVaultTreeEntryKind.Directory, null));
                    if (depth + 1 >= MaximumDepth)
                    {
                        state.IsTruncated = true;
                        continue;
                    }

                    var nested = Traverse(resolved.Value.FullPath, resolver, resolved.Value.RelativePath, depth + 1, entries, state);
                    if (!nested.IsSuccess || state.IsTruncated)
                    {
                        return nested;
                    }
                    continue;
                }

                if (File.Exists(fullPath))
                {
                    var size = new FileInfo(fullPath).Length;
                    if (size > MaximumTreeFileBytes)
                    {
                        state.HasOversizedEntries = true;
                        continue;
                    }
                    entries.Add(new KnowledgeVaultTreeEntry(resolved.Value!.RelativePath, KnowledgeVaultTreeEntryKind.File, size));
                }
            }

            return Result.Success();
        }
        catch (IOException)
        {
            return Result.Failure(new DomainError("vault.unavailable", "The knowledge vault checkout is unavailable."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure(new DomainError("vault.unavailable", "The knowledge vault checkout is unavailable."));
        }
    }

    private static Result CollectMarkdown(string directory, ProjectPathResolver resolver, string relativeDirectory, List<string> candidates, CandidateState state)
    {
        try
        {
            foreach (var fullPath in Directory.EnumerateFileSystemEntries(directory))
            {
                if (state.ExaminedEntries++ == MaximumCandidateFiles)
                {
                    state.IsTruncated = true;
                    return Result.Success();
                }

                if (IsReparsePoint(fullPath))
                {
                    continue;
                }

                var relativePath = string.IsNullOrEmpty(relativeDirectory) ? Path.GetFileName(fullPath) : $"{relativeDirectory}/{Path.GetFileName(fullPath)}";
                var resolved = resolver.Resolve(relativePath);
                if (!resolved.IsSuccess)
                {
                    continue;
                }

                if (Directory.Exists(fullPath))
                {
                    var nested = CollectMarkdown(resolved.Value!.FullPath, resolver, resolved.Value.RelativePath, candidates, state);
                    if (!nested.IsSuccess || state.IsTruncated)
                    {
                        return nested;
                    }
                }
                else if (File.Exists(fullPath) && IsMarkdown(resolved.Value!.RelativePath))
                {
                    if (new FileInfo(fullPath).Length > MaximumReadFileBytes)
                    {
                        state.IsTruncated = true;
                        continue;
                    }

                    candidates.Add(resolved.Value.RelativePath);
                }
            }
            return Result.Success();
        }
        catch (IOException)
        {
            return Result.Failure(new DomainError("vault.unavailable", "The knowledge vault checkout is unavailable."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure(new DomainError("vault.unavailable", "The knowledge vault checkout is unavailable."));
        }
    }

    private static string[] DecodeLines(byte[] bytes) => Utf8WithoutReplacement.GetString(bytes).TrimStart('\uFEFF').Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
    private static bool IsMarkdown(string path) => path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && !path.EndsWith("/", StringComparison.Ordinal);
    private static bool IsReparsePoint(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    private sealed record VaultContext(string Root, ProjectPathResolver Resolver);
    private sealed class TreeState { public int ExaminedEntries { get; set; } public bool IsTruncated { get; set; } public bool HasOversizedEntries { get; set; } }
    private sealed class CandidateState { public int ExaminedEntries { get; set; } public bool IsTruncated { get; set; } }
}
