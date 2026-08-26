namespace PatchPony.IntegrationTests;
public sealed class ObservabilityArtifactTests
{
    [Fact]
    public void ObservabilityStack_IsPrivateAndCoversPilotOperationalSignals()
    {
        var root = FindRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(root, "deploy", "observability", "docker-compose.yml"));
        var queries = File.ReadAllText(Path.Combine(root, "deploy", "observability", "prometheus", "postgres-queries.yaml"));
        var alerts = File.ReadAllText(Path.Combine(root, "deploy", "observability", "prometheus", "alerts.yml"));
        var dashboard = File.ReadAllText(Path.Combine(root, "deploy", "observability", "grafana", "dashboards", "patchpony-pilot.json"));
        Assert.Contains("127.0.0.1:13000:3000", compose, StringComparison.Ordinal);
        Assert.Contains("internal: true", compose, StringComparison.Ordinal);
        Assert.Contains("GRAFANA_ADMIN_PASSWORD", compose, StringComparison.Ordinal);
        Assert.Contains("patchpony_job_queue_depth", queries, StringComparison.Ordinal);
        Assert.Contains("patchpony_sessions_expired_unresolved", queries, StringComparison.Ordinal);
        Assert.Contains("PatchPonyJobQueueStalled", alerts, StringComparison.Ordinal);
        Assert.Contains("PatchPonyDatabaseGrowth", alerts, StringComparison.Ordinal);
        Assert.Contains("PatchPony Pilot Operations", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("0.0.0.0", compose, StringComparison.Ordinal);
    }
    private static string FindRepositoryRoot() { for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent) if (File.Exists(Path.Combine(d.FullName, "PLAN.md"))) return d.FullName; throw new DirectoryNotFoundException(); }
}