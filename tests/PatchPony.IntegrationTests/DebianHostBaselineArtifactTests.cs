namespace PatchPony.IntegrationTests;

public sealed class DebianHostBaselineArtifactTests
{
    [Fact]
    public void Debian13Baseline_RequiresExplicitSafetyConfirmationAndVerifiesItsOwnBoundaries()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "deploy", "host", "debian-13-baseline.sh"));

        Assert.Contains("VERSION_ID:-}\" == \"13", script, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY_SSH_ADMIN_USER", script, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY_CONFIRM_SSH_HARDENING", script, StringComparison.Ordinal);
        Assert.Contains("I_HAVE_A_SECOND_KEY_SESSION", script, StringComparison.Ordinal);
        Assert.Contains("PermitRootLogin no", script, StringComparison.Ordinal);
        Assert.Contains("PasswordAuthentication no", script, StringComparison.Ordinal);
        Assert.Contains("KbdInteractiveAuthentication no", script, StringComparison.Ordinal);
        Assert.Contains("PubkeyAuthentication yes", script, StringComparison.Ordinal);
        Assert.Contains("unattended-upgrades auditd apparmor apparmor-utils chrony", script, StringComparison.Ordinal);
        Assert.Contains("sshd -t", script, StringComparison.Ordinal);
        Assert.Contains("aa-status --enabled", script, StringComparison.Ordinal);
        Assert.Contains("timedatectl show -p NTPSynchronized", script, StringComparison.Ordinal);
        Assert.DoesNotContain("curl", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wget", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ufw", script, StringComparison.OrdinalIgnoreCase);
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