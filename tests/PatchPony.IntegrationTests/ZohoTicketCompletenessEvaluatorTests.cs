using Microsoft.Extensions.Configuration;
using PatchPony.Core.Workflows;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class ZohoTicketCompletenessEvaluatorTests
{
    [Fact]
    public void Evaluate_ReturnsOnlyStableMissingCriteria()
    {
        var evaluator = CreateEvaluator(40, 1, "steps", "expected");
        var ticket = CreateTicket("short", []);
        var result = evaluator.Evaluate(ticket);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsComplete);
        Assert.Equal(["attachments.minimum_count", "description.minimum_length", "description.required_phrase:expected", "description.required_phrase:steps"], result.Value.MissingCriteria);
    }

    [Fact]
    public void Evaluate_AcceptsACompleteTicketAndRejectsMissingPolicy()
    {
        var attachment = NormalizedTicketAttachment.Create("ATT-1", "error.log", "text/plain", 50).Value!;
        var ticket = CreateTicket("Steps: open app. Expected: login.", [attachment]);
        Assert.True(CreateEvaluator(10, 1, "steps", "expected").Evaluate(ticket).Value!.IsComplete);
        Assert.Equal("ticket.completeness.unconfigured", new ZohoTicketCompletenessEvaluator(new ConfigurationBuilder().Build()).Evaluate(ticket).Error.Code);
    }

    private static ZohoTicketCompletenessEvaluator CreateEvaluator(int minimumLength, int minimumAttachments, params string[] phrases)
    {
        var values = new Dictionary<string, string?> { ["PatchPony:Zoho:CompletenessPolicies:0:ProjectManifestId"] = "patchpony", ["PatchPony:Zoho:CompletenessPolicies:0:MinimumDescriptionLength"] = minimumLength.ToString(), ["PatchPony:Zoho:CompletenessPolicies:0:MinimumAttachmentCount"] = minimumAttachments.ToString() };
        for (var index = 0; index < phrases.Length; index++) values[$"PatchPony:Zoho:CompletenessPolicies:0:RequiredDescriptionPhrases:{index}"] = phrases[index];
        return new ZohoTicketCompletenessEvaluator(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }

    private static ProjectMappedTicket CreateTicket(string description, IReadOnlyList<NormalizedTicketAttachment> attachments) => ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "ZOHO-42", "17", "Title", description, DateTimeOffset.UtcNow, attachments).Value!, "patchpony").Value!;
}