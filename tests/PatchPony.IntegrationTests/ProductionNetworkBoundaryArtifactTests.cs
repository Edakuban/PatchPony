namespace PatchPony.IntegrationTests;

public sealed class ProductionNetworkBoundaryArtifactTests
{
    [Fact]
    public void ProductionNetworkArtifacts_ExposeOnlyTheHostCaddyEdge()
    {
        var root = FindRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(root, "docker-compose.yml"));
        var overrideFile = File.ReadAllText(Path.Combine(root, "deploy", "production", "docker-compose.rootless.yml"));
        var firewall = File.ReadAllText(Path.Combine(root, "deploy", "host", "nftables-edge-firewall.sh"));
        var caddy = File.ReadAllText(Path.Combine(root, "deploy", "caddy", "Caddyfile.host.production"));

        Assert.Contains("data:" + Environment.NewLine + "    internal: true", compose, StringComparison.Ordinal);
        Assert.Contains("network_mode: none", compose, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1:18080:8080", overrideFile, StringComparison.Ordinal);
        Assert.Contains("profiles: [\"local\"]", overrideFile, StringComparison.Ordinal);
        Assert.Contains("PATCHPONY_CONFIRM_FIREWALL", firewall, StringComparison.Ordinal);
        Assert.Contains("policy drop", firewall, StringComparison.Ordinal);
        Assert.Contains("tcp dport { 80, 443 }", firewall, StringComparison.Ordinal);
        Assert.Contains("forward", firewall, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1:18080", caddy, StringComparison.Ordinal);
        Assert.Contains("Strict-Transport-Security", caddy, StringComparison.Ordinal);
        Assert.DoesNotContain("docker.sock", caddy, StringComparison.OrdinalIgnoreCase);
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