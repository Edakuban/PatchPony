using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Audit;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Persistence;

namespace PatchPony.IntegrationTests;

public sealed class EfAuditEventWriterTests
{
    [Fact]
    public async Task Append_WritesAnAuditEvent()
    {
        var options = new DbContextOptionsBuilder<PatchPonyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var project = Project.Create(ProjectId.New(), "Pilot", DateTimeOffset.UtcNow).Value!;

        await using var database = new PatchPonyDbContext(options);
        await new EfProjectRepository(database).AddAsync(project, null);
        var auditEvent = new AuditEvent(Guid.NewGuid(), project.Id, null, "job.created", "correlation-42", DateTimeOffset.UtcNow, "{}");

        await new EfAuditEventWriter(database).AppendAsync(auditEvent);

        var persisted = await database.AuditEvents.SingleAsync();
        Assert.Equal(auditEvent.Id, persisted.Id);
        Assert.Equal(auditEvent.EventType, persisted.EventType);
        Assert.Equal(auditEvent.MetadataJson, persisted.MetadataJson);
    }
}
