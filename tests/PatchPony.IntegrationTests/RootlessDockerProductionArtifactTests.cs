namespace PatchPony.IntegrationTests;

public sealed class RootlessDockerProductionArtifactTests
{
    [Fact]
    public void RootlessProductionArtifacts_RequireDedicatedUserAndFailClosedVerification()
    {
        var root = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(root, "deploy", "host", "rootless-docker.sh"));
        var unit = File.ReadAllText(Path.Combine(root, "deploy", "systemd", "patchpony-sandbox-worker.service"));
        var dockerfile = File.ReadAllText(Path.Combine(root, "src", "PatchPony.Worker", "Dockerfile"));

        Assert.Contains("PATCHPONY_DOCKER_USER", script, StringComparison.Ordinal);
        Assert.Contains("must not belong to the docker group", script, StringComparison.Ordinal);
        Assert.Contains("rootful Docker daemon", script, StringComparison.Ordinal);
        Assert.Contains("uidmap dbus-user-session slirp4netns fuse-overlayfs", script, StringComparison.Ordinal);
        Assert.Contains("Delegate=cpu cpuset io memory pids", script, StringComparison.Ordinal);
        Assert.Contains("dockerd-rootless-setuptool.sh install", script, StringComparison.Ordinal);
        Assert.Contains("name=rootless", script, StringComparison.Ordinal);
        Assert.Contains("CgroupDriver", script, StringComparison.Ordinal);
        Assert.Contains("docker context show", script, StringComparison.Ordinal);
        Assert.DoesNotContain("curl", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wget", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Environment=DOCKER_HOST=unix:///run/user/%U/docker.sock", unit, StringComparison.Ordinal);
        Assert.Contains("--read-only --cap-drop=ALL --security-opt no-new-privileges:true --network none", unit, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY_WORKER_IMAGE", unit, StringComparison.Ordinal);
        Assert.Contains("docker-cli", dockerfile, StringComparison.Ordinal);
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