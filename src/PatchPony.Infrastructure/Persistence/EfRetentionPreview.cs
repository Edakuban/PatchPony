using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Retention;

namespace PatchPony.Infrastructure.Persistence;

public sealed class EfRetentionPreview(PatchPonyDbContext database) : IRetentionPreview
{
    public async Task<IReadOnlyList<RetentionCandidate>> GetExpiredAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        var sessions = await database.Sessions.AsNoTracking()
            .Where(session => session.ExpiresAt <= now)
            .Select(session => new RetentionCandidate(RetentionRecordType.Session, session.Id.ToString(), session.ExpiresAt))
            .ToListAsync(cancellationToken);
        var idempotencyRecords = await database.IdempotencyRecords.AsNoTracking()
            .Where(record => record.ExpiresAt <= now)
            .Select(record => new RetentionCandidate(RetentionRecordType.IdempotencyRecord, record.Key, record.ExpiresAt))
            .ToListAsync(cancellationToken);

        return sessions.Concat(idempotencyRecords)
            .OrderBy(candidate => candidate.ExpiresAt)
            .ThenBy(candidate => candidate.Type)
            .Take(maximumCount)
            .ToList();
    }
}
