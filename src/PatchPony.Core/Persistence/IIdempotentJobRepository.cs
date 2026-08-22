using PatchPony.Core.Jobs;
using PatchPony.Core.Workflows;

namespace PatchPony.Core.Persistence;

public sealed record IdempotentJobResult(Job Job, bool WasCreated);

public interface IIdempotentJobRepository
{
    Task<IdempotentJobResult> GetOrCreateAsync(
        IdempotencyRecord record,
        Job newJob,
        CancellationToken cancellationToken = default);
}
