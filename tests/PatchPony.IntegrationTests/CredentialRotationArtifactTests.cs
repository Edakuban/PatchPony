namespace PatchPony.IntegrationTests;

public sealed class CredentialRotationArtifactTests
{
    [Fact]
    public void RotationArtifact_ValidatesCandidatesAndUsesControlledRollback()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "deploy", "host", "rotate-credentials.sh"));
        var documentation = File.ReadAllText(Path.Combine(root, "docs", "credential-rotation.md"));

        Assert.Contains("set -Eeuo pipefail", script, StringComparison.Ordinal);
        Assert.Contains("umask 077", script, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY_CONFIRM_CREDENTIAL_ROTATION", script, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY_N8N_ROTATION_CONFIRMED", script, StringComparison.Ordinal);
        Assert.Contains("POSTGRES_PASSWORD", script, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY__AUTH__N8N__TOKEN", script, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY__WORKER__CLAIMSIGNINGKEY", script, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY__AI__APIKEY", script, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY__ZOHO__WEBHOOKSECRET", script, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY__ZOHO__ACCESSTOKEN", script, StringComparison.Ordinal);
        Assert.Contains("base64 --decode", script, StringComparison.Ordinal);
        Assert.Contains("install -m 0600", script, StringComparison.Ordinal);
        Assert.Contains("trap rollback EXIT", script, StringComparison.Ordinal);
        Assert.Contains("ALTER ROLE", script, StringComparison.Ordinal);
        Assert.Contains("--force-recreate gateway", script, StringComparison.Ordinal);
        Assert.Contains("systemctl --user restart patchpony-sandbox-worker.service", script, StringComparison.Ordinal);
        Assert.DoesNotContain("set -x", script, StringComparison.Ordinal);
        Assert.DoesNotContain("cat \"${RUNTIME_ENV}\"", script, StringComparison.Ordinal);
        Assert.Contains("Open WebUI", documentation, StringComparison.Ordinal);
        Assert.Contains("GitHub", documentation, StringComparison.Ordinal);
        Assert.Contains("never prints values", documentation, StringComparison.Ordinal);
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