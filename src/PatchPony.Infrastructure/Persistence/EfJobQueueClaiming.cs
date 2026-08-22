using System.Data;
using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Jobs;
using PatchPony.Core.Queue;

namespace PatchPony.Infrastructure.Persistence;

public sealed partial class EfJobQueue
{
    public Task<JobClaim?> TryClaimAsync(
        string workerId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default) =>
        database.Database.IsNpgsql()
            ? TryClaimPostgreSqlAsync(workerId, now, leaseDuration, cancellationToken)
            : TryClaimInMemoryAsync(workerId, now, leaseDuration, cancellationToken);

    private async Task<JobClaim?> TryClaimPostgreSqlAsync(
        string workerId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var claimId = Guid.NewGuid();
        var expiresAt = now.Add(leaseDuration);
        var connection = database.Database.GetDbConnection();
        var closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                WITH next_item AS (
                    SELECT "JobId"
                    FROM patchpony.job_queue
                    WHERE "AvailableAt" <= @now
                      AND ("ClaimExpiresAt" IS NULL OR "ClaimExpiresAt" <= @now)
                    ORDER BY "AvailableAt", "EnqueuedAt"
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                )
                UPDATE patchpony.job_queue AS queue
                SET "ClaimId" = @claimId,
                    "ClaimedBy" = @workerId,
                    "ClaimExpiresAt" = @expiresAt
                FROM next_item
                WHERE queue."JobId" = next_item."JobId"
                RETURNING queue."JobId", queue."ClaimId", queue."ClaimExpiresAt";
                """;
            AddParameter(command, "now", now.UtcDateTime);
            AddParameter(command, "claimId", claimId);
            AddParameter(command, "workerId", workerId);
            AddParameter(command, "expiresAt", expiresAt.UtcDateTime);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new JobClaim(reader.GetGuid(1), new JobId(reader.GetGuid(0)), workerId, reader.GetFieldValue<DateTimeOffset>(2));
        }
        finally
        {
            if (closeWhenDone)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<JobClaim?> TryClaimInMemoryAsync(
        string workerId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        var item = await database.JobQueue
            .Where(candidate => candidate.AvailableAt <= now && (candidate.ClaimExpiresAt == null || candidate.ClaimExpiresAt <= now))
            .OrderBy(candidate => candidate.AvailableAt)
            .ThenBy(candidate => candidate.EnqueuedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        var claim = new JobClaim(Guid.NewGuid(), new JobId(item.JobId), workerId, now.Add(leaseDuration));
        item.ClaimId = claim.ClaimId;
        item.ClaimedBy = claim.WorkerId;
        item.ClaimExpiresAt = claim.ExpiresAt;
        await database.SaveChangesAsync(cancellationToken);
        return claim;
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
