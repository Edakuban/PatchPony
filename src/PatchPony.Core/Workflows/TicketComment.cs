using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public sealed record TicketCommentLink(string Label, Uri Target);

/// <summary>A bounded, ticket-content-free delivery payload for an external task comment.</summary>
public sealed record TicketComment(string Outcome, IReadOnlyList<TicketCommentLink> Links)
{
    public static Result<TicketComment> Create(string outcome, IReadOnlyList<TicketCommentLink>? links = null)
    {
        var safeLinks = links ?? [];
        if (!IsText(outcome, 2_000) || safeLinks.Count > 8 || safeLinks.Any(link => link is null || !IsText(link.Label, 80) || !IsAllowedLink(link.Target)) || safeLinks.Select(link => link.Target.AbsoluteUri).Distinct(StringComparer.Ordinal).Count() != safeLinks.Count)
            return Result<TicketComment>.Failure(new DomainError("ticket.comment.invalid", "The ticket comment data is invalid."));
        return Result<TicketComment>.Success(new TicketComment(outcome, safeLinks.ToArray()));
    }

    public string RenderMarkdown()
    {
        if (Links.Count == 0) return Outcome;
        return Outcome + "\n\nLinks:\n" + string.Join("\n", Links.Select(link => $"- [{link.Label}]({link.Target.AbsoluteUri})"));
    }

    private static bool IsText(string value, int maximumLength) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength && value.All(character => !char.IsControl(character) || character is '\r' or '\n' or '\t');
    private static bool IsAllowedLink(Uri value) => value.IsAbsoluteUri && value.Scheme == Uri.UriSchemeHttps && !string.IsNullOrWhiteSpace(value.Host) && string.IsNullOrEmpty(value.UserInfo) && string.IsNullOrEmpty(value.Fragment) && value.AbsoluteUri.Length <= 1_024;
}