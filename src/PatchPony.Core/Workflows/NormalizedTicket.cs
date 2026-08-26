using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public enum ExternalTicketProvider { Zoho }
public enum TicketChangeType { Created, Updated }
public enum TicketChannel { GenericTask, FeatureRequest, Bug, ChangeRequest }

public sealed record NormalizedTicketAttachment(string ExternalId, string FileName, string ContentType, long SizeBytes)
{
    public static Result<NormalizedTicketAttachment> Create(string externalId, string fileName, string contentType, long sizeBytes)
    {
        if (!IsIdentifier(externalId) || string.IsNullOrWhiteSpace(fileName) || fileName.Length > 180 || fileName != Path.GetFileName(fileName) || fileName.Any(char.IsControl) || !AllowedTypes.Contains(contentType) || sizeBytes is < 1 or > 2 * 1024 * 1024 || IsArchive(fileName))
            return Result<NormalizedTicketAttachment>.Failure(new DomainError("ticket.attachment.invalid", "The ticket attachment metadata is invalid."));
        return Result<NormalizedTicketAttachment>.Success(new NormalizedTicketAttachment(externalId, fileName, contentType, sizeBytes));
    }

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal) { "text/plain", "text/markdown", "application/json", "application/yaml", "application/x-yaml", "text/yaml" };
    private static bool IsIdentifier(string value) => !string.IsNullOrEmpty(value) && value.Length <= 128 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    private static bool IsArchive(string fileName) => fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".tar", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".7z", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Provider-neutral, bounded ticket data for all subsequent workflow steps.</summary>
public sealed record NormalizedTicket(ExternalTicketProvider Provider, TicketChangeType ChangeType, string ExternalId, string Revision, string Title, string Description, IReadOnlyList<NormalizedTicketAttachment> Attachments, DateTimeOffset ReceivedAt, TicketChannel Channel)
{
    public static Result<NormalizedTicket> Create(ExternalTicketProvider provider, TicketChangeType changeType, string externalId, string revision, string title, string description, DateTimeOffset receivedAt, IReadOnlyList<NormalizedTicketAttachment>? attachments = null, TicketChannel channel = TicketChannel.GenericTask)
    {
        var safeAttachments = attachments ?? [];
        if (!IsIdentifier(externalId) || !IsIdentifier(revision) || !IsText(title, 500, false) || !IsText(description, 16 * 1024, true) || receivedAt == default || safeAttachments.Count > 5 || safeAttachments.Sum(attachment => attachment.SizeBytes) > 5 * 1024 * 1024 || safeAttachments.Select(attachment => attachment.ExternalId).Distinct(StringComparer.Ordinal).Count() != safeAttachments.Count)
            return Result<NormalizedTicket>.Failure(new DomainError("ticket.normalization.invalid", "The normalized ticket data is invalid."));
        return Result<NormalizedTicket>.Success(new NormalizedTicket(provider, changeType, externalId, revision, title, description, safeAttachments.ToArray(), receivedAt, channel));
    }

    private static bool IsIdentifier(string value) => !string.IsNullOrEmpty(value) && value.Length <= 128 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    private static bool IsText(string value, int maximumLength, bool allowNewlines) => !string.IsNullOrEmpty(value) && value.Length <= maximumLength && value.All(character => !char.IsControl(character) || (allowNewlines && character is '\r' or '\n' or '\t'));
}