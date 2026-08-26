using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class TicketInformationRequestTests
{
    [Fact]
    public void Create_RendersDeterministicQuestionsForKnownCriteria()
    {
        var result = TicketInformationRequest.Create(Triage(["attachments.minimum_count", "description.minimum_length", "description.required_phrase:steps"]));
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Questions.Count);
        Assert.Contains("Schritten zur Reproduktion", result.Value.Questions[1], StringComparison.Ordinal);
        Assert.Contains("steps", result.Value.Questions[2], StringComparison.Ordinal);
    }

    [Fact]
    public void Create_RejectsCompleteOrUnknownTriageCriteria()
    {
        var ticket = Ticket();
        var key = ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17").Value!;
        Assert.False(TicketInformationRequest.Create(TicketTriageOutput.Create(ticket, key, TicketCompletenessAssessment.Complete()).Value!).IsSuccess);
        Assert.False(TicketInformationRequest.Create(Triage(["unknown.criterion"])).IsSuccess);
    }

    private static TicketTriageOutput Triage(IReadOnlyList<string> criteria) => TicketTriageOutput.Create(Ticket(), ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17").Value!, TicketCompletenessAssessment.Incomplete(criteria)).Value!;
    private static ProjectMappedTicket Ticket() => ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "ZOHO-42", "17", "Title", "Description", DateTimeOffset.UtcNow).Value!, "patchpony").Value!;
}