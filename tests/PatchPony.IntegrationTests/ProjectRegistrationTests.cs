using Microsoft.EntityFrameworkCore;
using PatchPony.Core.Application;
using PatchPony.Infrastructure.Manifests;
using PatchPony.Infrastructure.Persistence;

namespace PatchPony.IntegrationTests;

public sealed class ProjectRegistrationTests
{
    [Fact]
    public async Task Register_PersistsManifestIdentityAndRepositoryConfiguration()
    {
        var options = new DbContextOptionsBuilder<PatchPonyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var manifest = new ProjectManifestLoader().Load(ValidManifest).Value!;

        await using (var database = new PatchPonyDbContext(options))
        {
            var projects = new EfProjectRepository(database);
            var result = await new ProjectRegistrationService(projects).RegisterAsync(manifest, DateTimeOffset.UtcNow);

            Assert.True(result.IsSuccess);
            Assert.Equal("patchpony", result.Value!.ManifestId);
            Assert.Equal("PatchPony", result.Value.Name);
        }

        await using (var database = new PatchPonyDbContext(options))
        {
            var projects = new EfProjectRepository(database);
            var project = await projects.GetByManifestIdAsync("patchpony");
            var repository = await projects.GetRepositoryAsync(project!.Id);

            Assert.NotNull(project);
            Assert.NotNull(repository);
            Assert.Equal("https://github.com/Edakuban/PatchPony.git", repository.RemoteUri.AbsoluteUri);
            Assert.Equal("main", repository.DefaultBranch);
        }
    }

    [Fact]
    public async Task Register_RejectsDuplicateManifestIdentity()
    {
        var options = new DbContextOptionsBuilder<PatchPonyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var manifest = new ProjectManifestLoader().Load(ValidManifest).Value!;

        await using var database = new PatchPonyDbContext(options);
        var service = new ProjectRegistrationService(new EfProjectRepository(database));

        Assert.True((await service.RegisterAsync(manifest, DateTimeOffset.UtcNow)).IsSuccess);
        var duplicate = await service.RegisterAsync(manifest, DateTimeOffset.UtcNow);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal("project.already_registered", duplicate.Error.Code);
    }

    private const string ValidManifest = """
        schemaVersion: 1
        project:
          id: patchpony
          displayName: PatchPony
        repository:
          remoteUrl: https://github.com/Edakuban/PatchPony.git
          defaultBranch: main
        paths:
          readable:
            - src/**
          writable: []
          forbidden:
            - .env
        """;
}
