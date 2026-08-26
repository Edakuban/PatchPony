using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public enum TicketTriageDisposition { NeedsInformation, ReadyForPlanning }

/// <summary>Stable, content-free triage contract shared by future workflow transports.</summary>
public sealed record TicketTriageOutput(
    string TicketExternalId,
    string TicketRevision,
    string ProjectManifestId,
    TicketChangeType ChangeType,
    string IdempotencyKey,
    bool IsComplete,
    IReadOnlyList<string> MissingCriteria,
    TicketTriageDisposition Disposition)
{
    public static Result<TicketTriageOutput> Create(ProjectMappedTicket ticket, string idempotencyKey, TicketCompletenessAssessment completeness)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(completeness);
        if (string.IsNullOrWhiteSpace(idempotencyKey) || !idempotencyKey.StartsWith("zoho:v1:", StringComparison.Ordinal) || idempotencyKey.Length != 72 || (completeness.IsComplete && completeness.MissingCriteria.Count != 0) || (!completeness.IsComplete && completeness.MissingCriteria.Count == 0))
            return Result<TicketTriageOutput>.Failure(new DomainError("ticket.triage.invalid", "The ticket triage input is invalid."));

        var missing = completeness.MissingCriteria.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var disposition = completeness.IsComplete ? TicketTriageDisposition.ReadyForPlanning : TicketTriageDisposition.NeedsInformation;
        return Result<TicketTriageOutput>.Success(new TicketTriageOutput(ticket.Ticket.ExternalId, ticket.Ticket.Revision, ticket.ProjectManifestId, ticket.Ticket.ChangeType, idempotencyKey, completeness.IsComplete, missing, disposition));
    }
}