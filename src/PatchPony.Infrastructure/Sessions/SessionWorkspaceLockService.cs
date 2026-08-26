using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PatchPony.Core.Common;
using PatchPony.Core.Sessions;

namespace PatchPony.Infrastructure.Sessions;

public enum SessionLockScope
{
    Project,
    Branch,
    WorkspacePath
}

public sealed record SessionLockLease(
    Guid LeaseId,
    SessionId OwnerSessionId,
    DateTimeOffset AcquiredAt,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<SessionLockScope> Scopes);

/// <summary>
/// Cross-process file leases for the short, mutating part of session provisioning.
/// All lock targets, including their paths and branch names, are derived on the server.
/// </summary>
public sealed class SessionWorkspaceLockService
{
    private static readonly TimeSpan MaximumLeaseDuration = TimeSpan.FromMinutes(15);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SessionWorkspaceLayoutResolver layoutResolver;

    public SessionWorkspaceLockService(SessionWorkspaceLayoutResolver layoutResolver)
    {
        this.layoutResolver = layoutResolver;
    }

    public async Task<Result<SessionLockLease>> AcquireAsync(
        Session session,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        if (session.Id.Value == Guid.Empty || session.ProjectId.Value == Guid.Empty || leaseDuration <= TimeSpan.Zero || leaseDuration > MaximumLeaseDuration)
        {
            return Result<SessionLockLease>.Failure(DomainError.Validation("A session with a project identifier and a lease duration up to fifteen minutes are required."));
        }

        var layout = layoutResolver.Resolve(session.Id);
        if (!SessionWorkspacePathGuard.IsSafePath(layout.StorageRoot, Path.Combine(layout.StorageRoot, "locks"), requireDirectory: false))
        {
            return Result<SessionLockLease>.Failure(new DomainError("session.lock.unsafe_path", "The controlled lock storage path is unsafe."));
        }

        var names = SessionNaming.For(session.Id);
        var lease = new SessionLockLease(Guid.NewGuid(), session.Id, now, now.Add(leaseDuration), [SessionLockScope.Project, SessionLockScope.Branch, SessionLockScope.WorkspacePath]);
        var targets = BuildTargets(layout, session.ProjectId, names.BranchName);
        var acquired = new List<LockTarget>();

        try
        {
            foreach (var target in targets.OrderBy(target => target.FilePath, StringComparer.Ordinal))
            {
                var result = await TryAcquireAsync(target, lease, now, cancellationToken);
                if (!result)
                {
                    await ReleaseTargetsAsync(acquired, lease, cancellationToken);
                    return Result<SessionLockLease>.Failure(new DomainError("session.lock.unavailable", "A project, branch, or workspace path is currently locked."));
                }

                acquired.Add(target);
            }

            return Result<SessionLockLease>.Success(lease);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await ReleaseTargetsAsync(acquired, lease, CancellationToken.None);
            throw;
        }
        catch
        {
            await ReleaseTargetsAsync(acquired, lease, CancellationToken.None);
            return Result<SessionLockLease>.Failure(new DomainError("session.lock.failed", "The session locks could not be acquired."));
        }
    }

    public async Task ReleaseAsync(Session session, SessionLockLease lease, CancellationToken cancellationToken = default)
    {
        if (session.Id != lease.OwnerSessionId)
        {
            return;
        }

        var layout = layoutResolver.Resolve(session.Id);
        var targets = BuildTargets(layout, session.ProjectId, SessionNaming.For(session.Id).BranchName);
        await ReleaseTargetsAsync(targets, lease, cancellationToken);
    }

    private static IReadOnlyList<LockTarget> BuildTargets(SessionWorkspaceLayout layout, PatchPony.Core.Projects.ProjectId projectId, string branchName)
    {
        var locksRoot = Path.Combine(layout.StorageRoot, "locks");
        return
        [
            new LockTarget(SessionLockScope.Project, projectId.Value.ToString("N"), Path.Combine(locksRoot, "projects", $"{projectId.Value:N}.lock")),
            new LockTarget(SessionLockScope.Branch, branchName, Path.Combine(locksRoot, "branches", $"{Hash(branchName)}.lock")),
            new LockTarget(SessionLockScope.WorkspacePath, layout.WorktreeRoot, Path.Combine(locksRoot, "paths", $"{Hash(layout.WorktreeRoot)}.lock"))
        ];
    }

    private static async Task<bool> TryAcquireAsync(LockTarget target, SessionLockLease lease, DateTimeOffset now, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target.FilePath)!);
        var document = new LockDocument(1, target.Scope, target.Resource, lease.OwnerSessionId.Value, lease.LeaseId, lease.AcquiredAt, lease.ExpiresAt);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                await using var stream = new FileStream(target.FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
                await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
                return true;
            }
            catch (IOException)
            {
                if (attempt == 1 || !await RetireIfExpiredAsync(target.FilePath, now, cancellationToken))
                {
                    return false;
                }
            }
        }

        return false;
    }

    private static async Task<bool> RetireIfExpiredAsync(string filePath, DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            LockDocument? document;
            await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                document = await JsonSerializer.DeserializeAsync<LockDocument>(stream, SerializerOptions, cancellationToken);
            }

            if (document is null || document.Version != 1 || document.ExpiresAt > now)
            {
                return false;
            }

            var retiredPath = $"{filePath}.{Guid.NewGuid():N}.expired";
            File.Move(filePath, retiredPath, overwrite: false);
            File.Delete(retiredPath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static async Task ReleaseTargetsAsync(IEnumerable<LockTarget> targets, SessionLockLease lease, CancellationToken cancellationToken)
    {
        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                LockDocument? document;
                await using (var stream = new FileStream(target.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                {
                    document = await JsonSerializer.DeserializeAsync<LockDocument>(stream, SerializerOptions, cancellationToken);
                }

                if (document?.OwnerSessionId == lease.OwnerSessionId.Value && document.LeaseId == lease.LeaseId)
                {
                    File.Delete(target.FilePath);
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (IOException)
            {
            }
            catch (JsonException)
            {
            }
        }
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record LockTarget(SessionLockScope Scope, string Resource, string FilePath);

    private sealed record LockDocument(
        int Version,
        SessionLockScope Scope,
        string Resource,
        Guid OwnerSessionId,
        Guid LeaseId,
        DateTimeOffset AcquiredAt,
        DateTimeOffset ExpiresAt);
}