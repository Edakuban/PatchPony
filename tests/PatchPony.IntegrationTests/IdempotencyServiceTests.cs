using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Application;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Persistence;

namespace PatchPony.IntegrationTests;

public sealed class IdempotencyServiceTests
{
    [Fact]
    public async Task CreateJobOnce_WithTheSameKey_ReturnsTheOriginalJobWithoutCreatingAnother()
    {
        var options = new DbContextOptionsBuilder<PatchPonyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var project = Project.Create(ProjectId.New(), "Pilot", DateTimeOffset.UtcNow).Value!;

        await using var database = new PatchPonyDbContext(options);
        var projects = new EfProjectRepository(database);
        await projects.AddAsync(project, null);
        var service = new IdempotencyService(projects, new EfIdempotentJobRepository(database));

        var first = await service.CreateJobOnceAsync(
            project.Id,
            "request-42",
            "triage",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(10));
        var second = await service.CreateJobOnceAsync(
            project.Id,
            "request-42",
            "triage",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(10));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(first.Value!.WasCreated);
        Assert.False(second.Value!.WasCreated);
        Assert.Equal(first.Value.Job.Id, second.Value.Job.Id);
        Assert.Single(await database.Jobs.ToListAsync());
        Assert.Single(await database.IdempotencyRecords.ToListAsync());
    }
}
