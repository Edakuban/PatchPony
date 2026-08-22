namespace PatchPony.Core.Retention;

public enum RetentionRecordType
{
    Session,
    IdempotencyRecord
}

public sealed record RetentionCandidate(RetentionRecordType Type, string Reference, DateTimeOffset ExpiresAt);

public interface IRetentionPreview
{
    Task<IReadOnlyList<RetentionCandidate>> GetExpiredAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken = default);
}
