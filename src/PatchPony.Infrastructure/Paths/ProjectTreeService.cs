using PatchPony.Core.Common;
using PatchPony.Core.Projects;

namespace PatchPony.Infrastructure.Paths;

public enum ProjectTreeEntryKind
{
    File,
    Directory
}

public sealed record ProjectTreeEntry(string RelativePath, ProjectTreeEntryKind Kind, long? SizeBytes);

public sealed record ProjectTreeResult(
    IReadOnlyList<ProjectTreeEntry> Entries,
    bool IsTruncated,
    bool HasOversizedEntries);

public sealed class ProjectTreeService
{
    private readonly ProjectPathResolver resolver;
    private readonly ProjectPathAccessService access;
    private readonly int maximumDepth;
    private readonly int maximumEntries;
    private readonly long maximumFileBytes;

    public ProjectTreeService(
        ProjectPathResolver resolver,
        ProjectPathAccessService access,
        int maximumDepth = 5,
        int maximumEntries = 500,
        long maximumFileBytes = 1_048_576)
    {
        if (maximumDepth is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        }

        if (maximumEntries is < 1 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));
        }

        if (maximumFileBytes is < 1 or > 100_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFileBytes));
        }

        this.resolver = resolver;
        this.access = access;
        this.maximumDepth = maximumDepth;
        this.maximumEntries = maximumEntries;
        this.maximumFileBytes = maximumFileBytes;
    }

    public Result<ProjectTreeResult> List()
    {
        var root = resolver.Resolve(".");
        if (!root.IsSuccess || !Directory.Exists(root.Value!.FullPath))
        {
            return Result<ProjectTreeResult>.Failure(new DomainError("tree.unavailable", "The project checkout tree is unavailable."));
        }

        var entries = new List<ProjectTreeEntry>();
        var state = new TraversalState();
        var traversal = Traverse(root.Value.FullPath, string.Empty, 0, entries, state);
        return traversal.IsSuccess
            ? Result<ProjectTreeResult>.Success(new ProjectTreeResult(entries, state.IsTruncated, state.HasOversizedEntries))
            : Result<ProjectTreeResult>.Failure(traversal.Error);
    }

    private Result Traverse(string directory, string relativeDirectory, int depth, List<ProjectTreeEntry> entries, TraversalState state)
    {
        try
        {
            foreach (var fullPath in Directory.EnumerateFileSystemEntries(directory))
            {
                if (state.ExaminedEntries == maximumEntries)
                {
                    state.IsTruncated = true;
                    return Result.Success();
                }

                state.ExaminedEntries++;
                var name = Path.GetFileName(fullPath);
                var relativePath = string.IsNullOrEmpty(relativeDirectory) ? name : $"{relativeDirectory}/{name}";
                var isDirectory = Directory.Exists(fullPath);

                if (isDirectory)
                {
                    var authorizedDirectory = access.ResolveDirectoryAndAuthorize(relativePath);
                    if (!authorizedDirectory.IsSuccess || IsReparsePoint(fullPath))
                    {
                        continue;
                    }

                    entries.Add(new ProjectTreeEntry(authorizedDirectory.Value!.RelativePath, ProjectTreeEntryKind.Directory, null));
                    if (depth + 1 >= maximumDepth)
                    {
                        state.IsTruncated = true;
                        continue;
                    }

                    var nested = Traverse(authorizedDirectory.Value.FullPath, authorizedDirectory.Value.RelativePath, depth + 1, entries, state);
                    if (!nested.IsSuccess || state.IsTruncated)
                    {
                        return nested;
                    }

                    continue;
                }

                var authorizedFile = access.ResolveAndAuthorize(relativePath, ProjectPathAccess.Read);
                if (!authorizedFile.IsSuccess || !File.Exists(fullPath))
                {
                    continue;
                }

                var fileInfo = new FileInfo(authorizedFile.Value!.FullPath);
                if (fileInfo.Length > maximumFileBytes)
                {
                    state.HasOversizedEntries = true;
                    continue;
                }

                entries.Add(new ProjectTreeEntry(authorizedFile.Value.RelativePath, ProjectTreeEntryKind.File, fileInfo.Length));
            }

            return Result.Success();
        }
        catch (IOException)
        {
            return Result.Failure(new DomainError("tree.unavailable", "The project checkout tree is unavailable."));
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure(new DomainError("tree.unavailable", "The project checkout tree is unavailable."));
        }
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private sealed class TraversalState
    {
        public int ExaminedEntries { get; set; }

        public bool IsTruncated { get; set; }

        public bool HasOversizedEntries { get; set; }
    }
}
