using PatchPony.Core.Application;
using PatchPony.Core.Jobs;
using PatchPony.Core.Persistence;
using PatchPony.Core.Projects;
using PatchPony.Core.Queue;

namespace PatchPony.Core.Tests;

public sealed class JobQueueServiceTests
{
    [Fact]
    public async Task Enqueue_RejectsAnUnknownJob()
    {
        var service = new JobQueueService(new InMemoryJobRepository(), new InMemoryJobQueue());
        var now = DateTimeOffset.UtcNow;

        var result = await service.EnqueueAsync(JobId.New(), now, now);

        Assert.False(result.IsSuccess);
        Assert.Equal("job.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Enqueue_ReturnsFalseWhenTheJobIsAlreadyQueued()
    {
        var jobs = new InMemoryJobRepository();
        var job = Job.Create(JobId.New(), ProjectId.New(), "triage", DateTimeOffset.UtcNow).Value!;
        await jobs.AddAsync(job);
        var service = new JobQueueService(jobs, new InMemoryJobQueue());
        var now = DateTimeOffset.UtcNow;

        var first = await service.EnqueueAsync(job.Id, now, now);
        var second = await service.EnqueueAsync(job.Id, now, now);

        Assert.True(first.IsSuccess);
        Assert.True(first.Value);
        Assert.True(second.IsSuccess);
        Assert.False(second.Value);
    }

    private sealed class InMemoryJobRepository : IJobRepository
    {
        private readonly Dictionary<JobId, Job> jobs = [];

        public Task<Job?> GetAsync(JobId id, CancellationToken cancellationToken = default) => Task.FromResult(jobs.GetValueOrDefault(id));

        public Task AddAsync(Job job, CancellationToken cancellationToken = default)
        {
            jobs.Add(job.Id, job);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Job job, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryJobQueue : IJobQueue
    {
        private readonly Dictionary<JobId, JobQueueEntry> entries = [];

        public Task<bool> TryEnqueueAsync(JobQueueEntry entry, CancellationToken cancellationToken = default) =>
            Task.FromResult(entries.TryAdd(entry.JobId, entry));

        public Task<JobClaim?> TryClaimAsync(string workerId, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            var entry = entries.Values.FirstOrDefault(candidate => candidate.AvailableAt <= now);
            return Task.FromResult(entry is null ? null : new JobClaim(Guid.NewGuid(), entry.JobId, workerId, now.Add(leaseDuration)));
        }

        public Task<IReadOnlyList<JobQueueEntry>> GetReadyAsync(DateTimeOffset now, int maximumCount, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<JobQueueEntry>>(
                entries.Values.Where(entry => entry.AvailableAt <= now).OrderBy(entry => entry.AvailableAt).Take(maximumCount).ToList());
    }
}
