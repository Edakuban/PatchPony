using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

/// <summary>Hard stop for feature requests that involve source changes; they can only be planned and researched.</summary>
public static class FeatureRequestSourceChangePolicy
{
    public static Result<TicketAutomationDecision> Decide(TicketChangeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Channel != TicketChannel.FeatureRequest || request.Kind != RequestedChangeKind.SourceCode)
            return Result<TicketAutomationDecision>.Failure(new DomainError("feature.source_policy.invalid", "The feature source change policy applies only to source feature requests."));
        return Result<TicketAutomationDecision>.Success(TicketAutomationDecision.PlanOnly("automation.feature_source_plan_only"));
    }
}