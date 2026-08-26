using System.Text;
using System.Text.RegularExpressions;
using PatchPony.Core.Common;

namespace PatchPony.Core.Publication;

public sealed record PublicationTestEvidence(string Name, string Outcome);
public sealed record MergeRequestDescriptionInput(string TicketReference, string PlanSummary, string DiffSummary, IReadOnlyList<PublicationTestEvidence> Tests, IReadOnlyList<string> Risks);

public static class GitMergeRequestDescription
{
    private static readonly Regex TicketPattern = new("^[A-Za-z0-9][A-Za-z0-9._/-]{0,127}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static Result<string> Create(MergeRequestDescriptionInput input)
    {
        if (input is null || !TicketPattern.IsMatch(input.TicketReference ?? string.Empty) || !Bounded(input.PlanSummary, 4_000) || !Bounded(input.DiffSummary, 8_000) || input.Tests is null || input.Tests.Count > 50 || input.Risks is null || input.Risks.Count > 50 || input.Tests.Any(test => !Bounded(test.Name, 200) || !Bounded(test.Outcome, 100)) || input.Risks.Any(risk => !Bounded(risk, 500)))
            return Result<string>.Failure(DomainError.Validation("Bounded publication evidence is required for a merge request description."));

        var text = new StringBuilder();
        text.AppendLine("## PatchPony change proposal");
        text.AppendLine(); text.AppendLine($"- Ticket: `{input.TicketReference}`");
        text.AppendLine(); text.AppendLine("## Plan"); text.AppendLine(input.PlanSummary.Trim());
        text.AppendLine(); text.AppendLine("## Diff summary"); text.AppendLine(input.DiffSummary.Trim());
        text.AppendLine(); text.AppendLine("## Tests");
        foreach (var test in input.Tests) text.AppendLine($"- `{test.Name.Trim()}`: {test.Outcome.Trim()}");
        text.AppendLine(); text.AppendLine("## Risks");
        if (input.Risks.Count == 0) text.AppendLine("- None recorded.");
        else foreach (var risk in input.Risks) text.AppendLine($"- {risk.Trim()}");
        return Result<string>.Success(text.ToString().TrimEnd());
    }
    private static bool Bounded(string? value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum && !value.Any(char.IsControl);
}