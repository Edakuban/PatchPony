using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class TicketPlanMarkdownRendererTests
{
    [Fact]
    public void Render_ProducesDeterministicReadablePlanMarkdown()
    {
        var rendered = TicketPlanMarkdownRenderer.Render(Plan());
        Assert.True(rendered.IsSuccess);
        Assert.Equal("plan.md", rendered.Value!.FileName);
        Assert.Contains("# PatchPony Plan", rendered.Value.Markdown, StringComparison.Ordinal);
        Assert.Contains("## Evidenz", rendered.Value.Markdown, StringComparison.Ordinal);
        Assert.Contains("1. Freigegebene Projektquellen", rendered.Value.Markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("Description", rendered.Value.Markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_RejectsMalformedPlans()
    {
        var plan = Plan() with { Steps = [] };
        Assert.False(TicketPlanMarkdownRenderer.Render(plan).IsSuccess);
    }

    private static TicketPlanOutput Plan()
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "ZOHO-42", "17", "Title", "Description", DateTimeOffset.UtcNow).Value!, "patchpony").Value!;
        var triage = TicketTriageOutput.Create(ticket, ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17").Value!, TicketCompletenessAssessment.Complete()).Value!;
        var evidence = new[] { TicketPlanEvidence.Create(TicketPlanEvidenceKind.Source, "src/App.cs:1-4").Value! };
        return TicketPlanOutput.Create(triage, evidence).Value!;
    }
}