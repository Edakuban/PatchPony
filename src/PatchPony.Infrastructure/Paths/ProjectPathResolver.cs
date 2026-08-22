using PatchPony.Core.Common;

namespace PatchPony.Infrastructure.Paths;

public sealed record ResolvedProjectPath(string RelativePath, string FullPath);

public sealed class ProjectPathResolver
{
    private readonly string checkoutRoot;

    public ProjectPathResolver(string checkoutRoot)
    {
        if (!Path.IsPathFullyQualified(checkoutRoot))
        {
            throw new ArgumentException("The checkout root must be an absolute server-side path.", nameof(checkoutRoot));
        }

        this.checkoutRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(checkoutRoot));
    }

    public Result<ResolvedProjectPath> Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || relativePath.Contains('\\') || HasTraversalSegment(relativePath))
        {
            return Result<ResolvedProjectPath>.Failure(new DomainError("path.invalid", "A safe relative project path is required."));
        }

        string candidate;
        try
        {
            candidate = Path.GetFullPath(relativePath, checkoutRoot);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Result<ResolvedProjectPath>.Failure(new DomainError("path.invalid", "A safe relative project path is required."));
        }

        var canonicalRelativePath = Path.GetRelativePath(checkoutRoot, candidate);
        if (!IsInsideCheckout(canonicalRelativePath))
        {
            return Result<ResolvedProjectPath>.Failure(new DomainError("path.outside_checkout", "The requested path is outside the project checkout."));
        }

        var symlinkCheck = EnsureExistingSegmentsStayInsideCheckout(canonicalRelativePath);
        if (!symlinkCheck.IsSuccess)
        {
            return Result<ResolvedProjectPath>.Failure(symlinkCheck.Error);
        }

        return Result<ResolvedProjectPath>.Success(new ResolvedProjectPath(
            ToPortablePath(canonicalRelativePath),
            candidate));
    }

    private Result EnsureExistingSegmentsStayInsideCheckout(string canonicalRelativePath)
    {
        var current = checkoutRoot;
        foreach (var segment in canonicalRelativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            var info = GetExistingFileSystemInfo(current);
            if (info is null)
            {
                break;
            }

            if ((info.Attributes & FileAttributes.ReparsePoint) == 0)
            {
                continue;
            }

            try
            {
                var target = info.ResolveLinkTarget(returnFinalTarget: true);
                if (target is null || !IsInsideCheckout(Path.GetRelativePath(checkoutRoot, Path.GetFullPath(target.FullName))))
                {
                    return Result.Failure(new DomainError("path.symlink_outside", "The requested path leaves the project checkout through a link."));
                }
            }
            catch (IOException)
            {
                return Result.Failure(new DomainError("path.symlink_invalid", "The requested path contains an unresolved link."));
            }
            catch (UnauthorizedAccessException)
            {
                return Result.Failure(new DomainError("path.symlink_invalid", "The requested path contains an inaccessible link."));
            }
        }

        return Result.Success();
    }

    private static FileSystemInfo? GetExistingFileSystemInfo(string path)
    {
        if (Directory.Exists(path))
        {
            return new DirectoryInfo(path);
        }

        return File.Exists(path) ? new FileInfo(path) : null;
    }

    private static bool HasTraversalSegment(string path) =>
        path.Split('/', StringSplitOptions.None).Any(segment => segment == "..");

    private static bool IsInsideCheckout(string canonicalRelativePath) =>
        !Path.IsPathRooted(canonicalRelativePath) &&
        canonicalRelativePath != ".." &&
        !canonicalRelativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
        !canonicalRelativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);

    private static string ToPortablePath(string path) =>
        path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
}