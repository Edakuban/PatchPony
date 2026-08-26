using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class TicketPlanOutputTests
{
    [Fact]
    public void Create_UsesOnlyValidatedEvidenceAndFixedPlanPhases()
    {
        var triage = ReadyTriage();
        var evidence = new[]
        {
            TicketPlanEvidence.Create(TicketPlanEvidenceKind.Skill, ".patchpony/skills/validate.md").Value!,
            TicketPlanEvidence.Create(TicketPlanEvidenceKind.Source, "src/App/Settings.cs:10-20").Value!,
            TicketPlanEvidence.Create(TicketPlanEvidenceKind.Config, "config/settings.json:1-8").Value!,
            TicketPlanEvidence.Create(TicketPlanEvidenceKind.Knowledge, "vault/runbook.md:4-12").Value!
        };
        var result = TicketPlanOutput.Create(triage, evidence);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Steps.Count);
        Assert.Equal("review-evidence", result.Value.Steps[0].Id);
        Assert.Equal(4, result.Value.Evidence.Count);
    }

    [Fact]
    public void Create_RejectsIncompleteTriageAndUnsafeEvidence()
    {
        Assert.False(TicketPlanEvidence.Create(TicketPlanEvidenceKind.Source, "../.env").IsSuccess);
        var incomplete = TicketTriageOutput.Create(Ticket(), ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17").Value!, TicketCompletenessAssessment.Incomplete(["description.minimum_length"])).Value!;
        var evidence = TicketPlanEvidence.Create(TicketPlanEvidenceKind.Source, "src/App.cs:1").Value!;
        Assert.False(TicketPlanOutput.Create(incomplete, [evidence]).IsSuccess);
        Assert.False(TicketPlanOutput.Create(ReadyTriage(), [evidence, evidence]).IsSuccess);
    }

    private static TicketTriageOutput ReadyTriage() => TicketTriageOutput.Create(Ticket(), ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17").Value!, TicketCompletenessAssessment.Complete()).Value!;
    private static ProjectMappedTicket Ticket() => ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "ZOHO-42", "17", "Title", "Description", DateTimeOffset.UtcNow).Value!, "patchpony").Value!;
}