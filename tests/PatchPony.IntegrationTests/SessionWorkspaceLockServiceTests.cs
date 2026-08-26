using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Sessions;

namespace PatchPony.IntegrationTests;

public sealed class SessionWorkspaceLockServiceTests
{
    [Fact]
    public async Task Acquire_LocksProjectBranchAndWorkspaceUntilReleased()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var projectId = ProjectId.New();
            var first = CreateSession(projectId, now);
            var second = CreateSession(projectId, now);
            var service = CreateService(root);

            var lease = await service.AcquireAsync(first, now, TimeSpan.FromMinutes(2));
            var conflicting = await service.AcquireAsync(second, now, TimeSpan.FromMinutes(2));

            Assert.True(lease.IsSuccess);
            Assert.Equal(3, Directory.GetFiles(Path.Combine(root, "storage", "locks"), "*.lock", SearchOption.AllDirectories).Length);
            Assert.False(conflicting.IsSuccess);
            Assert.Equal("session.lock.unavailable", conflicting.Error.Code);

            await service.ReleaseAsync(first, lease.Value!);
            var afterRelease = await service.AcquireAsync(second, now, TimeSpan.FromMinutes(2));
            Assert.True(afterRelease.IsSuccess);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Acquire_ReclaimsExpiredLeaseAndRejectsInvalidDurations()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var projectId = ProjectId.New();
            var first = CreateSession(projectId, now);
            var second = CreateSession(projectId, now);
            var service = CreateService(root);

            var expired = await service.AcquireAsync(first, now, TimeSpan.FromSeconds(1));
            var reclaimed = await service.AcquireAsync(second, now.AddSeconds(2), TimeSpan.FromMinutes(2));
            var invalid = await service.AcquireAsync(second, now, TimeSpan.FromMinutes(16));

            Assert.True(expired.IsSuccess);
            Assert.True(reclaimed.IsSuccess);
            Assert.False(invalid.IsSuccess);
            Assert.Equal("validation.invalid", invalid.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Release_DoesNotRemoveALeaseOwnedByAnotherSession()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var projectId = ProjectId.New();
            var first = CreateSession(projectId, now);
            var second = CreateSession(ProjectId.New(), now);
            var service = CreateService(root);
            var lease = await service.AcquireAsync(first, now, TimeSpan.FromMinutes(2));

            await service.ReleaseAsync(second, lease.Value!);
            var stillLocked = await service.AcquireAsync(CreateSession(projectId, now), now, TimeSpan.FromMinutes(2));

            Assert.True(lease.IsSuccess);
            Assert.False(stillLocked.IsSuccess);
            Assert.Equal("session.lock.unavailable", stillLocked.Error.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static SessionWorkspaceLockService CreateService(string root) => new(new SessionWorkspaceLayoutResolver(Path.Combine(root, "storage")));

    private static Session CreateSession(ProjectId projectId, DateTimeOffset now) =>
        Session.Create(SessionId.New(), projectId, JobId.New(), now, now.AddHours(1)).Value!;

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "PatchPony.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}