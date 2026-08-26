using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Jobs;
using PatchPony.Core.Publication;
using PatchPony.Core.Workflows;

namespace PatchPony.Infrastructure.Persistence;

public sealed class EfApprovalRepository(PatchPonyDbContext database) : IApprovalRepository
{
    public async Task<Approval?> GetLatestAsync(JobId jobId, CancellationToken cancellationToken = default)
    {
        var row = await database.Approvals.AsNoTracking().Where(approval => approval.JobId == jobId.Value).OrderByDescending(approval => approval.RequestedAt).ThenByDescending(approval => approval.Id).FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : new Approval(row.Id, new JobId(row.JobId), Enum.Parse<ApprovalDecision>(row.Decision, ignoreCase: false), row.RequestedBy, row.RequestedAt);
    }
}