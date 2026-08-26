using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public enum TicketFailureDisposition { Retry, Escalate }

public sealed record TicketWorkflowFailure(TicketFailureDisposition Disposition, string ReasonCode, string UserMessage)
{
    public static TicketWorkflowFailure From(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return IsRetryable(error.Code)
            ? new TicketWorkflowFailure(TicketFailureDisposition.Retry, error.Code, "Die Verarbeitung wird kontrolliert erneut versucht.")
            : new TicketWorkflowFailure(TicketFailureDisposition.Escalate, error.Code, "Die Anfrage benötigt eine manuelle Untersuchung.");
    }

    private static bool IsRetryable(string code) => code is "git_provider.request_failed" or "publication.push_failed" or "publication.commit_failed" or "zoho.provider.request_failed";
}

/// <summary>Content-free manual escalation record; no ticket text, secrets or provider response is retained here.</summary>
public sealed record TicketEscalationRequest(string TicketExternalId, string TicketRevision, string ProjectManifestId, string IdempotencyKey, string ReasonCode)
{
    public static Result<TicketEscalationRequest> Create(TicketTriageOutput triage, TicketWorkflowFailure failure)
    {
        ArgumentNullException.ThrowIfNull(triage);
        ArgumentNullException.ThrowIfNull(failure);
        if (failure.Disposition != TicketFailureDisposition.Escalate || string.IsNullOrWhiteSpace(failure.ReasonCode)) return Result<TicketEscalationRequest>.Failure(new DomainError("ticket.escalation.invalid", "The workflow failure cannot be escalated."));
        return Result<TicketEscalationRequest>.Success(new TicketEscalationRequest(triage.TicketExternalId, triage.TicketRevision, triage.ProjectManifestId, triage.IdempotencyKey, failure.ReasonCode));
    }
}