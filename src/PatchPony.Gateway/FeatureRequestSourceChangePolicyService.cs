using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class FeatureRequestSourceChangePolicyService
{
    public Result<TicketAutomationDecision> Decide(TicketChangeRequest request) => FeatureRequestSourceChangePolicy.Decide(request);
}