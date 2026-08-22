using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Application;
using PatchPony.Core.Projects;
using PatchPony.Infrastructure.Persistence;

namespace PatchPony.IntegrationTests;

public sealed class KnowledgeSourceRegistrationTests
{
    [Fact]
    public async Task RegisterVault_PersistsASeparateProjectBoundSource()
    {
        var options = new DbContextOptionsBuilder<PatchPonyDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        var project = Project.Create(ProjectId.New(), "patchpony", "PatchPony", DateTimeOffset.UtcNow).Value!;

        await using var database = new PatchPonyDbContext(options);
        await new EfProjectRepository(database).AddAsync(project, null);
        var sources = new EfKnowledgeSourceRepository(database);
        var service = new KnowledgeSourceRegistrationService(new EfProjectRepository(database), sources);

        var registered = await service.RegisterVaultAsync(project.Id, new Uri("https://github.com/example/vault.git"), "main", DateTimeOffset.UtcNow);
        var persisted = await sources.GetAsync(registered.Value!.Id);
        var duplicate = await service.RegisterVaultAsync(project.Id, new Uri("https://github.com/example/other-vault.git"), "main", DateTimeOffset.UtcNow);

        Assert.True(registered.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(project.Id, persisted!.ProjectId);
        Assert.Equal("vault", persisted.Name);
        Assert.False(duplicate.IsSuccess);
        Assert.Equal("knowledge_source.already_registered", duplicate.Error.Code);
    }
}
