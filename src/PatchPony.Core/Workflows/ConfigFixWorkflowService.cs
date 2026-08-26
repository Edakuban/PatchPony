using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

public sealed record ConfigFixPipelineRequest(string ProjectManifestId, string IdempotencyKey);
public sealed record ConfigFixPipelineResult(string SessionReference, string MergeRequestReference);

/// <summary>Closed adapter boundary; implementations may only use existing session, validation, test, approval and MR controls.</summary>
public interface IConfigFixPipeline
{
    Task<Result<string>> CreateSessionAsync(ConfigFixPipelineRequest request, CancellationToken cancellationToken = default);
    Task<Result> ApplyValidatedPatchAsync(string sessionReference, CancellationToken cancellationToken = default);
    Task<Result> RunRegisteredTestsAsync(string sessionReference, CancellationToken cancellationToken = default);
    Task<Result> EnsureApprovedAsync(ConfigFixPipelineRequest request, CancellationToken cancellationToken = default);
    Task<Result<string>> CreateMergeRequestAsync(string sessionReference, CancellationToken cancellationToken = default);
}

public sealed class ConfigFixWorkflowService
{
    public async Task<Result<ConfigFixPipelineResult>> ExecuteAsync(TicketAutomationDecision decision, TicketPlanOutput plan, IConfigFixPipeline pipeline, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(pipeline);
        if (decision.Outcome != TicketWorkflowOutcome.ChangeProposalReady)
            return Result<ConfigFixPipelineResult>.Failure(new DomainError("ticket.config_fix.not_allowed", "The hard automation policy did not allow a config fix."));

        var request = new ConfigFixPipelineRequest(plan.ProjectManifestId, plan.IdempotencyKey);
        var session = await pipeline.CreateSessionAsync(request, cancellationToken);
        if (!session.IsSuccess) return Result<ConfigFixPipelineResult>.Failure(session.Error);
        var patched = await pipeline.ApplyValidatedPatchAsync(session.Value!, cancellationToken);
        if (!patched.IsSuccess) return Result<ConfigFixPipelineResult>.Failure(patched.Error);
        var tested = await pipeline.RunRegisteredTestsAsync(session.Value!, cancellationToken);
        if (!tested.IsSuccess) return Result<ConfigFixPipelineResult>.Failure(tested.Error);
        var approved = await pipeline.EnsureApprovedAsync(request, cancellationToken);
        if (!approved.IsSuccess) return Result<ConfigFixPipelineResult>.Failure(approved.Error);
        var mergeRequest = await pipeline.CreateMergeRequestAsync(session.Value!, cancellationToken);
        return mergeRequest.IsSuccess
            ? Result<ConfigFixPipelineResult>.Success(new ConfigFixPipelineResult(session.Value!, mergeRequest.Value!))
            : Result<ConfigFixPipelineResult>.Failure(mergeRequest.Error);
    }
}