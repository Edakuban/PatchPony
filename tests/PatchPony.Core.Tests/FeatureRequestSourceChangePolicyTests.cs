using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class FeatureRequestSourceChangePolicyTests
{
    [Fact]
    public void Decide_AlwaysReturnsPlanOnlyForFeatureSourceChanges()
    {
        var result = FeatureRequestSourceChangePolicy.Decide(Request(TicketChannel.FeatureRequest, RequestedChangeKind.SourceCode));

        Assert.True(result.IsSuccess);
        Assert.Equal(TicketWorkflowOutcome.PlanOnly, result.Value!.Outcome);
        Assert.Equal("automation.feature_source_plan_only", result.Value.ReasonCode);
    }

    [Theory]
    [InlineData(TicketChannel.ChangeRequest, RequestedChangeKind.SourceCode)]
    [InlineData(TicketChannel.FeatureRequest, RequestedChangeKind.Configuration)]
    public void Decide_RejectsRequestsOutsideItsNarrowScope(TicketChannel channel, RequestedChangeKind kind)
    {
        var result = FeatureRequestSourceChangePolicy.Decide(Request(channel, kind));

        Assert.False(result.IsSuccess);
        Assert.Equal("feature.source_policy.invalid", result.Error.Code);
    }

    private static TicketChangeRequest Request(TicketChannel channel, RequestedChangeKind kind)
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "1362699000036844130", "17", "Title", "Description", DateTimeOffset.UtcNow, channel: channel).Value!, "patchpony").Value!;
        return TicketChangeRequest.Create(ticket, kind, [RequestedChangeTarget.Create(kind == RequestedChangeKind.SourceCode ? "src/PatchPony.Core/Feature.cs" : "config/enabled", kind == RequestedChangeKind.SourceCode ? null : "true").Value!]).Value!;
    }
}