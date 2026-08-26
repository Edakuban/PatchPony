using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

/// <summary>Produces a bounded status-only task comment after all change-request policies have passed.</summary>
public static class ChangeRequestCommentFactory
{
    public static Result<TicketComment> Create(EnvironmentConfigurationChangeRequest request, ConfigurationReviewRequirement review)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(review);
        if (review.ExplicitHumanApprovalRequiredBeforePublication != request.Policy.ExplicitHumanApprovalRequiredBeforePublication || (request.Policy.ReviewerRequired && review.Reviewers.Count == 0) || (!request.Policy.ReviewerRequired && review.Reviewers.Count != 0))
            return Result<TicketComment>.Failure(new DomainError("change_request.comment.invalid", "The change request comment input is invalid."));

        var outcome = review.ExplicitHumanApprovalRequiredBeforePublication
            ? "Die Konfigurationsänderung wurde geprüft. Vor einer Veröffentlichung ist eine explizite menschliche Freigabe erforderlich."
            : review.Reviewers.Count > 0
                ? "Die Konfigurationsänderung wurde geprüft und wartet auf das konfigurierte Review."
                : "Die Konfigurationsänderung wurde geprüft und kann kontrolliert weiterverarbeitet werden.";
        return TicketComment.Create(outcome);
    }
}