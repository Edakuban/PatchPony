using PatchPony.Core.Common;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Jobs;

public readonly record struct JobId(Guid Value)
{
    public static JobId New() => new(Guid.NewGuid());
}

public sealed record JobStatusChanged(JobId JobId, JobStatus From, JobStatus To, DateTimeOffset OccurredAt);

public sealed class Job
{
    private static readonly IReadOnlyDictionary<JobStatus, HashSet<JobStatus>> AllowedTransitions =
        new Dictionary<JobStatus, HashSet<JobStatus>>
        {
            [JobStatus.Received] = [JobStatus.Triaging, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.Triaging] = [JobStatus.NeedsInformation, JobStatus.Ready, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.NeedsInformation] = [JobStatus.Triaging, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.Ready] = [JobStatus.Planning, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.Planning] = [JobStatus.NeedsInformation, JobStatus.Planned, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.Planned] = [JobStatus.Implementing, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.Implementing] = [JobStatus.Validating, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.Validating] = [JobStatus.ReviewReady, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.ReviewReady] = [JobStatus.Committed, JobStatus.Closed, JobStatus.Failed, JobStatus.Cancelled],
            [JobStatus.Committed] = [JobStatus.MergeRequestCreated, JobStatus.Failed],
            [JobStatus.MergeRequestCreated] = [JobStatus.Closed, JobStatus.Failed]
        };

    private readonly List<JobStatusChanged> statusChanges = [];

    private Job(JobId id, ProjectId projectId, string kind, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        Kind = kind;
        CreatedAt = createdAt;
        Status = JobStatus.Received;
    }

    public JobId Id { get; }

    public ProjectId ProjectId { get; }

    public string Kind { get; }

    public JobStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyList<JobStatusChanged> StatusChanges => statusChanges;

    public static Result<Job> Create(JobId id, ProjectId projectId, string kind, DateTimeOffset createdAt)
    {
        if (id.Value == Guid.Empty || projectId.Value == Guid.Empty || string.IsNullOrWhiteSpace(kind) || kind.Length > 100)
        {
            return Result<Job>.Failure(DomainError.Validation("Job id, project id and a job kind of at most 100 characters are required."));
        }

        return Result<Job>.Success(new Job(id, projectId, kind.Trim(), createdAt));
    }

    public static Job Rehydrate(JobId id, ProjectId projectId, string kind, JobStatus status, DateTimeOffset createdAt)
    {
        var job = new Job(id, projectId, kind, createdAt)
        {
            Status = status
        };

        return job;
    }

    public Result TransitionTo(JobStatus target, DateTimeOffset occurredAt)
    {
        if (target == Status)
        {
            return Result.Failure(DomainError.Conflict("job.transition.noop", "A job cannot transition to its current status."));
        }

        if (!AllowedTransitions.TryGetValue(Status, out var targets) || !targets.Contains(target))
        {
            return Result.Failure(DomainError.Conflict(
                "job.transition.invalid",
                $"Transition from {Status} to {target} is not allowed."));
        }

        var previousStatus = Status;
        Status = target;
        statusChanges.Add(new JobStatusChanged(Id, previousStatus, target, occurredAt));
        return Result.Success();
    }

    public Result Fail(DateTimeOffset occurredAt) => TransitionTo(JobStatus.Failed, occurredAt);

    public Result Cancel(DateTimeOffset occurredAt) => TransitionTo(JobStatus.Cancelled, occurredAt);

    public Result Close(DateTimeOffset occurredAt) => TransitionTo(JobStatus.Closed, occurredAt);
}
