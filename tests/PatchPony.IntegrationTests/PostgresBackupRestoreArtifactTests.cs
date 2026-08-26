namespace PatchPony.IntegrationTests;

public sealed class PostgresBackupRestoreArtifactTests
{
    [Fact]
    public void RestoreRehearsal_IsolatedAndVerifiesARealArchiveRestore()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "deploy", "host", "postgres-backup-restore-test.sh"));
        var documentation = File.ReadAllText(Path.Combine(root, "docs", "postgresql-backup-restore.md"));

        Assert.Contains("set -Eeuo pipefail", script, StringComparison.Ordinal);
        Assert.Contains("trap cleanup EXIT", script, StringComparison.Ordinal);
        Assert.Contains("pg_dump --format=custom", script, StringComparison.Ordinal);
        Assert.Contains("pg_restore --list", script, StringComparison.Ordinal);
        Assert.Contains("pg_restore --clean --if-exists", script, StringComparison.Ordinal);
        Assert.Contains("--exit-on-error", script, StringComparison.Ordinal);
        Assert.Contains("sha256sum /backup/patchpony.dump", script, StringComparison.Ordinal);
        Assert.Contains("restore_probe", script, StringComparison.Ordinal);
        Assert.Contains("docker volume rm \"${SOURCE_VOLUME}\" \"${TARGET_VOLUME}\" \"${BACKUP_VOLUME}\"", script, StringComparison.Ordinal);
        Assert.DoesNotContain(".env", script, StringComparison.Ordinal);
        Assert.DoesNotContain("postgres-data", script, StringComparison.Ordinal);
        Assert.DoesNotContain("system prune", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does **not** read `.env`", documentation, StringComparison.Ordinal);
        Assert.Contains("PatchPony PostgreSQL backup/restore rehearsal: PASS", documentation, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PLAN.md"))) return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the PatchPony repository root.");
    }
}