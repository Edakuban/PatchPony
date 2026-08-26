namespace PatchPony.Core.Workflows;

/// <summary>Non-sensitive completeness outcome for a project-mapped bug report.</summary>
public sealed record TicketCompletenessAssessment(bool IsComplete, IReadOnlyList<string> MissingCriteria)
{
    public static TicketCompletenessAssessment Complete() => new(true, []);
    public static TicketCompletenessAssessment Incomplete(IEnumerable<string> criteria) => new(false, criteria.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
}