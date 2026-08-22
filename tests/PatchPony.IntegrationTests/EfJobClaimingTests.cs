using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Queue;
using PatchPony.Infrastructure.Persistence;

namespace PatchPony.IntegrationTests;

public sealed class EfJobClaimingTests
{
    [Fact]
    public async Task Claiming_GivesEachWorkerADifferentJobAndReclaimsExpiredLeases()
    {
        var options = new DbContextOptionsBuilder<PatchPonyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(ProjectId.New(), "Pilot", now).Value!;
        var firstJob = Job.Create(JobId.New(), project.Id, "triage", now).Value!;
        var secondJob = Job.Create(JobId.New(), project.Id, "triage", now).Value!;

        await using var database = new PatchPonyDbContext(options);
        await new EfProjectRepository(database).AddAsync(project, null);
        var jobs = new EfJobRepository(database);
        await jobs.AddAsync(firstJob);
        await jobs.AddAsync(secondJob);
        var queue = new EfJobQueue(database);
        await queue.TryEnqueueAsync(new JobQueueEntry(firstJob.Id, now, now));
        await queue.TryEnqueueAsync(new JobQueueEntry(secondJob.Id, now, now));

        var firstClaim = await queue.TryClaimAsync("worker-a", now, TimeSpan.FromMinutes(1));
        var secondClaim = await queue.TryClaimAsync("worker-b", now, TimeSpan.FromMinutes(1));
        var noClaim = await queue.TryClaimAsync("worker-c", now, TimeSpan.FromMinutes(1));
        var reclaimed = await queue.TryClaimAsync("worker-c", now.AddMinutes(2), TimeSpan.FromMinutes(1));

        Assert.NotNull(firstClaim);
        Assert.NotNull(secondClaim);
        Assert.NotEqual(firstClaim.JobId, secondClaim.JobId);
        Assert.Null(noClaim);
        Assert.NotNull(reclaimed);
        Assert.Equal(firstClaim.JobId, reclaimed.JobId);
        Assert.NotEqual(firstClaim.ClaimId, reclaimed.ClaimId);
    }
}
