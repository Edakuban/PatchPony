using PatchPony.Core.Common;

namespace PatchPony.Core.Knowledge;

public sealed record KnowledgeAttachmentMetadata(string RelativePath, long SizeBytes);

/// <summary>Default-deny metadata policy for non-Markdown vault attachments. Content inspection is intentionally out of scope.</summary>
public sealed class KnowledgeAttachmentPolicy
{
    public const long MaximumFileBytes = 8L * 1024 * 1024;
    public const int MaximumAttachmentCount = 100;
    public const long MaximumTotalBytes = 64L * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".pdf" };

    public Result Validate(KnowledgeAttachmentMetadata attachment)
    {
        if (!IsSafeRelativePath(attachment.RelativePath)) return Result.Failure(new DomainError("knowledge.attachment.invalid_path", "The attachment path must be a safe vault-relative path."));
        if (!AllowedExtensions.Contains(Path.GetExtension(attachment.RelativePath))) return Result.Failure(new DomainError("knowledge.attachment.extension_disallowed", "The attachment file type is not allowed."));
        if (attachment.SizeBytes is < 1 or > MaximumFileBytes) return Result.Failure(new DomainError("knowledge.attachment.too_large", "The attachment exceeds the per-file byte limit."));
        return Result.Success();
    }

    public Result ValidateCollection(IEnumerable<KnowledgeAttachmentMetadata> attachments)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        var count = 0;
        long total = 0;
        foreach (var attachment in attachments)
        {
            var valid = Validate(attachment);
            if (!valid.IsSuccess) return valid;
            if (++count > MaximumAttachmentCount) return Result.Failure(new DomainError("knowledge.attachment.too_many", "The attachment count exceeds the configured limit."));
            if (attachment.SizeBytes > MaximumTotalBytes - total) return Result.Failure(new DomainError("knowledge.attachment.total_too_large", "The attachment total exceeds the configured byte limit."));
            total += attachment.SizeBytes;
        }
        return Result.Success();
    }

    private static bool IsSafeRelativePath(string path) =>
        !string.IsNullOrWhiteSpace(path) && path.Length <= 512 && !Path.IsPathRooted(path) && !path.Contains('\\') && !path.Contains('?') && !path.Contains(':') && !path.Any(char.IsControl) && path.Split('/').All(segment => segment is not "" and not "." and not "..");
}