using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class TicketTriageOutputTests
{
    [Fact]
    public void Create_ProducesContentFreeReadyForPlanningOutput()
    {
        var ticket = Ticket();
        var key = ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17").Value!;
        var result = TicketTriageOutput.Create(ticket, key, TicketCompletenessAssessment.Complete());

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketTriageDisposition.ReadyForPlanning, result.Value!.Disposition);
        Assert.True(result.Value.IsComplete);
        Assert.Empty(result.Value.MissingCriteria);
        Assert.DoesNotContain("Description", string.Join(',', result.Value.GetType().GetProperties().Select(property => property.Name)), StringComparison.Ordinal);
    }

    [Fact]
    public void Create_UsesNeedsInformationForMissingCriteriaAndRejectsContradictions()
    {
        var key = ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17").Value!;
        var incomplete = TicketTriageOutput.Create(Ticket(), key, TicketCompletenessAssessment.Incomplete(["description.minimum_length", "attachments.minimum_count"]));

        Assert.True(incomplete.IsSuccess);
        Assert.Equal(TicketTriageDisposition.NeedsInformation, incomplete.Value!.Disposition);
        Assert.Equal(["attachments.minimum_count", "description.minimum_length"], incomplete.Value.MissingCriteria);
        Assert.False(TicketTriageOutput.Create(Ticket(), key, new TicketCompletenessAssessment(true, ["wrong"])).IsSuccess);
    }

    private static ProjectMappedTicket Ticket() => ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "ZOHO-42", "17", "Title", "Description", DateTimeOffset.UtcNow).Value!, "patchpony").Value!;
}