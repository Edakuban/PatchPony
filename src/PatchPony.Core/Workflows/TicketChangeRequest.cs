using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public enum RequestedChangeKind { Configuration, SourceCode, DatabaseMigration, Secret }

public sealed record RequestedChangeTarget(string Reference, string? RequestedValue)
{
    public static Result<RequestedChangeTarget> Create(string reference, string? requestedValue)
    {
        if (!IsReference(reference) || !IsValue(requestedValue))
            return Result<RequestedChangeTarget>.Failure(new DomainError("ticket.change_request.invalid", "The requested change target is invalid."));
        return Result<RequestedChangeTarget>.Success(new RequestedChangeTarget(reference, requestedValue));
    }

    private static bool IsReference(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 200 && !value.StartsWith("/", StringComparison.Ordinal) && !value.Contains((char)92) && !value.Contains("..", StringComparison.Ordinal) && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or '/');
    private static bool IsValue(string? value) => value is null || (!string.IsNullOrWhiteSpace(value) && value.Length <= 512 && value.All(character => !char.IsControl(character) || character is '\r' or '\n' or '\t'));
}

/// <summary>Bounded, typed change intent captured independently of any execution or policy decision.</summary>
public sealed record TicketChangeRequest(
    string TicketExternalId,
    string TicketRevision,
    string ProjectManifestId,
    TicketChannel Channel,
    RequestedChangeKind Kind,
    IReadOnlyList<RequestedChangeTarget> Targets)
{
    public static Result<TicketChangeRequest> Create(ProjectMappedTicket ticket, RequestedChangeKind kind, IReadOnlyList<RequestedChangeTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(targets);
        if (ticket.Ticket.Channel is not (TicketChannel.FeatureRequest or TicketChannel.ChangeRequest) || targets.Count is < 1 or > 16 || targets.Any(target => target is null) || targets.Select(target => target.Reference).Distinct(StringComparer.Ordinal).Count() != targets.Count || (kind == RequestedChangeKind.Secret && targets.Any(target => target.RequestedValue is not null)))
            return Result<TicketChangeRequest>.Failure(new DomainError("ticket.change_request.invalid", "The requested change is invalid."));
        return Result<TicketChangeRequest>.Success(new TicketChangeRequest(ticket.Ticket.ExternalId, ticket.Ticket.Revision, ticket.ProjectManifestId, ticket.Ticket.Channel, kind, targets.ToArray()));
    }
}