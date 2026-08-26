using PatchPony.Core.Common;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;

namespace PatchPony.Core.Publication;

public enum PublicationOperation { Commit, Push, MergeRequest }

/// <summary>Server-derived idempotency key; it never contains user-controlled data.</summary>
public sealed record PublicationOperationKey(ProjectId ProjectId, JobId JobId, SessionId SessionId, PublicationOperation Operation)
{
    public string Value => $"publication:{ProjectId.Value:N}:{JobId.Value:N}:{SessionId.Value:N}:{Operation}";
}

/// <summary>Retries only explicitly normalized transient publication failures.</summary>
public sealed class PublicationRetryPolicy
{
    private readonly int maximumAttempts;

    public PublicationRetryPolicy(int maximumAttempts = 3)
    {
        if (maximumAttempts is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        this.maximumAttempts = maximumAttempts;
    }

    public async Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await operation(cancellationToken);
            if (result.IsSuccess || attempt >= maximumAttempts || !IsTransient(result.Error.Code)) return result;
        }
    }

    public async Task<Result> ExecuteAsync(Func<CancellationToken, Task<Result>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await operation(cancellationToken);
            if (result.IsSuccess || attempt >= maximumAttempts || !IsTransient(result.Error.Code)) return result;
        }
    }

    private static bool IsTransient(string code) => code is "git_provider.request_failed" or "publication.push_failed" or "publication.commit_failed";
}