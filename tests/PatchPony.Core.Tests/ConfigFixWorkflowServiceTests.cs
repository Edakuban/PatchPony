using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Core.Tests;

public sealed class ConfigFixWorkflowServiceTests
{
    [Fact]
    public async Task Execute_RunsTheClosedPipelineInOrderOnlyAfterHardPolicyApproval()
    {
        var pipeline = new RecordingPipeline();
        var result = await new ConfigFixWorkflowService().ExecuteAsync(new TicketAutomationDecision(TicketWorkflowOutcome.ChangeProposalReady, "allowed"), Plan(), pipeline);
        Assert.True(result.IsSuccess);
        Assert.Equal(["session", "patch", "tests", "approval", "mr"], pipeline.Calls);
        Assert.Equal("mr-42", result.Value!.MergeRequestReference);
    }

    [Fact]
    public async Task Execute_StopsBeforeAnySideEffectWhenPolicyOrTestFails()
    {
        var denied = new RecordingPipeline();
        var result = await new ConfigFixWorkflowService().ExecuteAsync(new TicketAutomationDecision(TicketWorkflowOutcome.PlanOnly, "denied"), Plan(), denied);
        Assert.False(result.IsSuccess); Assert.Empty(denied.Calls);
        var failing = new RecordingPipeline { FailAt = "tests" };
        var failed = await new ConfigFixWorkflowService().ExecuteAsync(new TicketAutomationDecision(TicketWorkflowOutcome.ChangeProposalReady, "allowed"), Plan(), failing);
        Assert.False(failed.IsSuccess); Assert.Equal(["session", "patch", "tests"], failing.Calls);
    }

    private static TicketPlanOutput Plan()
    {
        var ticket = ProjectMappedTicket.Create(NormalizedTicket.Create(ExternalTicketProvider.Zoho, TicketChangeType.Created, "ZOHO-42", "17", "Title", "Description", DateTimeOffset.UtcNow).Value!, "patchpony").Value!;
        var triage = TicketTriageOutput.Create(ticket, ZohoWebhookIdempotencyKey.Create("task.created", "ZOHO-42", "17").Value!, TicketCompletenessAssessment.Complete()).Value!;
        return TicketPlanOutput.Create(triage, [TicketPlanEvidence.Create(TicketPlanEvidenceKind.Config, "config/app.json:1").Value!]).Value!;
    }

    private sealed class RecordingPipeline : IConfigFixPipeline
    {
        public List<string> Calls { get; } = []; public string? FailAt { get; init; }
        public Task<Result<string>> CreateSessionAsync(ConfigFixPipelineRequest request, CancellationToken cancellationToken = default) => Text("session", "session-42");
        public Task<Result> ApplyValidatedPatchAsync(string sessionReference, CancellationToken cancellationToken = default) => Plain("patch");
        public Task<Result> RunRegisteredTestsAsync(string sessionReference, CancellationToken cancellationToken = default) => Plain("tests");
        public Task<Result> EnsureApprovedAsync(ConfigFixPipelineRequest request, CancellationToken cancellationToken = default) => Plain("approval");
        public Task<Result<string>> CreateMergeRequestAsync(string sessionReference, CancellationToken cancellationToken = default) => Text("mr", "mr-42");
        private Task<Result> Plain(string call) { Calls.Add(call); return Task.FromResult(FailAt == call ? Result.Failure(new DomainError("test.failed", "failed")) : Result.Success()); }
        private Task<Result<string>> Text(string call, string value) { Calls.Add(call); return Task.FromResult(FailAt == call ? Result<string>.Failure(new DomainError("test.failed", "failed")) : Result<string>.Success(value)); }
    }
}