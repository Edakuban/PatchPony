using PatchPony.Core.Common;

namespace PatchPony.Core.Knowledge;

/// <summary>Hard deny rules for content that must never be exposed or processed as vault knowledge.</summary>
public sealed class KnowledgeVaultContentPolicy
{
    private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".com", ".dll", ".msi", ".bat", ".cmd", ".ps1", ".sh", ".bash", ".zsh", ".js", ".mjs", ".cjs", ".vbs", ".jar", ".py", ".rb", ".pl", ".php", ".lua"
    };

    public Result ValidateReadablePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || relativePath.Contains('\\') || relativePath.Contains(':') || relativePath.Any(char.IsControl))
        {
            return Result.Failure(new DomainError("knowledge.content.invalid_path", "The vault path must be a safe relative path."));
        }

        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            return Result.Failure(new DomainError("knowledge.content.invalid_path", "The vault path must be a safe relative path."));
        }

        if (segments.Length >= 2 && string.Equals(segments[0], ".obsidian", StringComparison.OrdinalIgnoreCase) && (string.Equals(segments[1], "plugins", StringComparison.OrdinalIgnoreCase) || string.Equals(segments[1], "snippets", StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(new DomainError("knowledge.content.protected_path", "Obsidian plugins and snippets are not knowledge content."));
        }

        return ExecutableExtensions.Contains(Path.GetExtension(relativePath))
            ? Result.Failure(new DomainError("knowledge.content.executable_disallowed", "Executable and script content is not allowed in the knowledge vault."))
            : Result.Success();
    }
}