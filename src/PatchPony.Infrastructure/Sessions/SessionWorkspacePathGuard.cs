namespace PatchPony.Infrastructure.Sessions;

/// <summary>Rejects reparse points in all existing components of a server-derived session path.</summary>
public static class SessionWorkspacePathGuard
{
    public static bool IsSafePath(string storageRoot, string candidate, bool requireDirectory)
    {
        if (!Path.IsPathFullyQualified(storageRoot) || !Path.IsPathFullyQualified(candidate))
        {
            return false;
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(storageRoot));
        var fullCandidate = Path.GetFullPath(candidate);
        var relative = Path.GetRelativePath(root, fullCandidate);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return false;
        }

        var current = root;
        if (!Directory.Exists(current) && !File.Exists(current))
        {
            return !requireDirectory;
        }

        if (!IsSafeExistingComponent(current))
        {
            return false;
        }

        foreach (var segment in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if ((Directory.Exists(current) || File.Exists(current)) && !IsSafeExistingComponent(current))
            {
                return false;
            }
        }

        return !requireDirectory || Directory.Exists(fullCandidate);
    }

    public static bool TryDeleteDirectoryTree(string storageRoot, string directory)
    {
        if (!IsSafePath(storageRoot, directory, requireDirectory: true))
        {
            return false;
        }

        try
        {
            return TryDeleteDirectoryTreeCore(directory);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryDeleteDirectoryTreeCore(string directory)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                return false;
            }

            if ((attributes & FileAttributes.Directory) != 0)
            {
                if (!TryDeleteDirectoryTreeCore(entry))
                {
                    return false;
                }
            }
            else
            {
                File.Delete(entry);
            }
        }

        Directory.Delete(directory);
        return true;
    }

    private static bool IsSafeExistingComponent(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}