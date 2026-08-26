using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class TicketAutomationDecisionTests
{
    [Fact]
    public void Create_AllowsOnlyConfigEvidenceWithRequiredEvidence()
    {
        var decision = TicketAutomationDecision.Create(Plan(TicketPlanEvidenceKind.Config, TicketPlanEvidenceKind.Skill), true, new HashSet<TicketPlanEvidenceKind> { TicketPlanEvidenceKind.Config, TicketPlanEvidenceKind.Skill });
        Assert.True(decision.IsSuccess);
        Assert.Equal(TicketWorkflowOutcome.ChangeProposalReady, decision.Value!.Outcome);
    }

    [Fact]
    public void Create_RestrictsSourceAndMissingPolicyEvidenceToPlanOnly()
    {
        var source = TicketAutomationDecision.Create(Plan(TicketPlanEvidenceKind.Source, TicketPlanEvidenceKind.Config), true, new HashSet<TicketPlanEvidenceKind>());
        var missing = TicketAutomationDecision.Create(Plan(TicketPlanEvidenceKind.Config), true, new HashSet<TicketPlanEvidenceKind> { TicketPlanEvidenceKind.Knowledge });
        Assert.Equal(TicketWorkflowOutcome.PlanOnly, source.Value!.Outcome);
        Assert.Equal("automation.source_change_not_allowed", source.Value.ReasonCode);
        Assert.Equal(TicketWorkflowOutcome.PlanOnly, missing.Value!.Outcome);
    }

    private static TicketPlanOutput Plan(params TicketPlanEvidenceKind[] kinds)
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "ZOHO-42", "17", "Title", "Description", DateTimeOffset.UtcNow).Value!, "patchpony").Value!;
        var triage = TicketTriageOutput.Create(ticket, ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17").Value!, TicketCompletenessAssessment.Complete()).Value!;
        var evidence = kinds.Select((kind, index) => TicketPlanEvidence.Create(kind, $"evidence-{index}").Value!).ToArray();
        return TicketPlanOutput.Create(triage, evidence).Value!;
    }
}