using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class TicketWorkflowFailureTests
{
    [Fact]
    public void From_RetriesOnlyExplicitTransientInfrastructureErrors()
    {
        Assert.Equal(TicketFailureDisposition.Retry, TicketWorkflowFailure.From(new DomainError("git_provider.request_failed", "transport details")).Disposition);
        var unknown = TicketWorkflowFailure.From(new DomainError("ticket.project_mapping.unmapped", "internal details"));
        Assert.Equal(TicketFailureDisposition.Escalate, unknown.Disposition);
        Assert.DoesNotContain("internal details", unknown.UserMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Escalation_ContainsOnlyTicketReferencesAndReasonCode()
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "ZOHO-42", "17", "Title", "Description", DateTimeOffset.UtcNow).Value!, "patchpony").Value!;
        var triage = TicketTriageOutput.Create(ticket, ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17").Value!, TicketCompletenessAssessment.Complete()).Value!;
        var escalation = TicketEscalationRequest.Create(triage, TicketWorkflowFailure.From(new DomainError("ticket.policy.denied", "secret")));
        Assert.True(escalation.IsSuccess);
        Assert.Equal("ticket.policy.denied", escalation.Value!.ReasonCode);
        Assert.DoesNotContain("Description", string.Join(',', escalation.Value.GetType().GetProperties().Select(property => property.Name)), StringComparison.Ordinal);
    }
}