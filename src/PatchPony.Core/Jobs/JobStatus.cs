namespace PatchPony.Core.Jobs;

public enum JobStatus
{
    Received,
    Triaging,
    NeedsInformation,
    Ready,
    Planning,
    Planned,
    Implementing,
    Validating,
    ReviewReady,
    Committed,
    MergeRequestCreated,
    Closed,
    Failed,
    Cancelled
}
