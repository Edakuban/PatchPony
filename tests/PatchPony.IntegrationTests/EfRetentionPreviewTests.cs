using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Application;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Retention;
using PatchPony.Infrastructure.Persistence;

namespace PatchPony.IntegrationTests;

public sealed class EfRetentionPreviewTests
{
    [Fact]
    public async Task PreviewExpired_ReturnsExpiredRecordsWithoutDeletingThem()
    {
        var options = new DbContextOptionsBuilder<PatchPonyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(ProjectId.New(), "Pilot", now).Value!;
        var job = Job.Create(JobId.New(), project.Id, "triage", now).Value!;

        await using var database = new PatchPonyDbContext(options);
        await new EfProjectRepository(database).AddAsync(project, null);
        await new EfJobRepository(database).AddAsync(job);
        database.Sessions.Add(new SessionRow
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id.Value,
            JobId = job.Id.Value,
            CreatedAt = now.AddHours(-2),
            ExpiresAt = now.AddHours(-1)
        });
        database.Sessions.Add(new SessionRow
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id.Value,
            JobId = job.Id.Value,
            CreatedAt = now,
            ExpiresAt = now.AddHours(1)
        });
        database.IdempotencyRecords.Add(new IdempotencyRecordRow
        {
            Key = "expired-request",
            ProjectId = project.Id.Value,
            JobId = job.Id.Value,
            CreatedAt = now.AddHours(-2),
            ExpiresAt = now.AddHours(-1)
        });
        await database.SaveChangesAsync();
        var service = new RetentionService(new EfRetentionPreview(database));

        var preview = await service.PreviewExpiredAsync(now, 10);

        Assert.Equal(2, preview.Count);
        Assert.Contains(preview, candidate => candidate.Type == RetentionRecordType.Session);
        Assert.Contains(preview, candidate => candidate.Type == RetentionRecordType.IdempotencyRecord);
        Assert.Equal(2, await database.Sessions.CountAsync());
        Assert.Single(await database.IdempotencyRecords.ToListAsync());
    }
}
