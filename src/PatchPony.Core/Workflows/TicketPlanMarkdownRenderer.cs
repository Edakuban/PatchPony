using System.Text;
using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public sealed record RenderedTicketPlan(string FileName, string Markdown);

/// <summary>Deterministic Markdown projection of a validated review-only ticket plan.</summary>
public static class TicketPlanMarkdownRenderer
{
    public const string FileName = "plan.md";

    public static Result<RenderedTicketPlan> Render(TicketPlanOutput plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.Steps.Count != 3 || plan.Evidence.Count is < 1 or > 16) return Result<RenderedTicketPlan>.Failure(new DomainError("ticket.plan.render_invalid", "The structured ticket plan cannot be rendered."));
        var markdown = new StringBuilder();
        markdown.AppendLine("# PatchPony Plan");
        markdown.AppendLine();
        markdown.AppendLine($"- Ticket: `{EscapeCode(plan.TicketExternalId)}`");
        markdown.AppendLine($"- Revision: `{EscapeCode(plan.TicketRevision)}`");
        markdown.AppendLine($"- Projekt: `{EscapeCode(plan.ProjectManifestId)}`");
        markdown.AppendLine($"- Idempotenz: `{EscapeCode(plan.IdempotencyKey)}`");
        markdown.AppendLine();
        markdown.AppendLine("## Evidenz");
        foreach (var evidence in plan.Evidence) markdown.AppendLine($"- {evidence.Kind}: `{EscapeCode(evidence.Reference)}`");
        markdown.AppendLine();
        markdown.AppendLine("## Schritte");
        for (var index = 0; index < plan.Steps.Count; index++)
        {
            var step = plan.Steps[index];
            markdown.AppendLine($"{index + 1}. {step.Title}");
            foreach (var evidence in step.Evidence) markdown.AppendLine($"   - {evidence.Kind}: `{EscapeCode(evidence.Reference)}`");
        }
        return Result<RenderedTicketPlan>.Success(new RenderedTicketPlan(FileName, markdown.ToString()));
    }

    private static string EscapeCode(string value) => value.Replace("`", "\\`").Replace("\r", string.Empty).Replace("\n", string.Empty);
}