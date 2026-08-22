using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Persistence;

namespace PatchPony.IntegrationTests;

public sealed class EfRepositoryTests
{
    [Fact]
    public async Task JobRepository_RehydratesAndPersistsStatusTransitions()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<PatchPonyDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var project = Project.Create(ProjectId.New(), "Pilot", DateTimeOffset.UtcNow).Value!;
        var job = Job.Create(JobId.New(), project.Id, "triage", DateTimeOffset.UtcNow).Value!;

        await using (var database = new PatchPonyDbContext(options))
        {
            var projects = new EfProjectRepository(database);
            var jobs = new EfJobRepository(database);
            await projects.AddAsync(project, null);
            await jobs.AddAsync(job);
        }

        await using (var database = new PatchPonyDbContext(options))
        {
            var jobs = new EfJobRepository(database);
            var rehydrated = await jobs.GetAsync(job.Id);

            Assert.NotNull(rehydrated);
            Assert.Equal(JobStatus.Received, rehydrated.Status);
            Assert.True(rehydrated.TransitionTo(JobStatus.Triaging, DateTimeOffset.UtcNow).IsSuccess);
            await jobs.UpdateAsync(rehydrated);
        }

        await using (var database = new PatchPonyDbContext(options))
        {
            var jobs = new EfJobRepository(database);
            var persisted = await jobs.GetAsync(job.Id);

            Assert.NotNull(persisted);
            Assert.Equal(JobStatus.Triaging, persisted.Status);
            Assert.Single(await database.JobStatusChanges.ToListAsync());
        }
    }
}
