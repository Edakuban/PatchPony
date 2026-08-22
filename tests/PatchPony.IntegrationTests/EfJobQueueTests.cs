using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Queue;
using PatchPony.Infrastructure.Persistence;

namespace PatchPony.IntegrationTests;

public sealed class EfJobQueueTests
{
    [Fact]
    public async Task Queue_ReturnsOnlyReadyJobsInAvailabilityOrder()
    {
        var options = new DbContextOptionsBuilder<PatchPonyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(ProjectId.New(), "Pilot", now).Value!;
        var readyJob = Job.Create(JobId.New(), project.Id, "triage", now).Value!;
        var delayedJob = Job.Create(JobId.New(), project.Id, "triage", now).Value!;

        await using var database = new PatchPonyDbContext(options);
        await new EfProjectRepository(database).AddAsync(project, null);
        var jobs = new EfJobRepository(database);
        await jobs.AddAsync(readyJob);
        await jobs.AddAsync(delayedJob);
        var queue = new EfJobQueue(database);
        Assert.True(await queue.TryEnqueueAsync(new JobQueueEntry(readyJob.Id, now, now)));
        Assert.True(await queue.TryEnqueueAsync(new JobQueueEntry(delayedJob.Id, now, now.AddMinutes(5))));

        var ready = await queue.GetReadyAsync(now, 10);

        Assert.Single(ready);
        Assert.Equal(readyJob.Id, ready[0].JobId);
        Assert.False(await queue.TryEnqueueAsync(new JobQueueEntry(readyJob.Id, now, now)));
    }
}
