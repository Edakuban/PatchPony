namespace PatchPony.IntegrationTests;

public sealed class OpenWebUiPipeArtifactTests
{
    [Fact]
    public void Pipe_ExposesTheKnowledgeModelAndMinimalN8nContract()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "integrations", "open-webui", "patchpony_n8n_pipe.py"));

        Assert.Contains("class Pipe:", source, StringComparison.Ordinal);
        Assert.Contains("async def pipe(", source, StringComparison.Ordinal);
        Assert.Contains("N8N_WEBHOOK_URL", source, StringComparison.Ordinal);
        Assert.Contains("N8N_WEBHOOK_TOKEN", source, StringComparison.Ordinal);
        Assert.Contains("DEFAULT_PROJECT_ID", source, StringComparison.Ordinal);
        Assert.Contains("owui-sha256:", source, StringComparison.Ordinal);
        Assert.Contains("hashlib.sha256", source, StringComparison.Ordinal);
        Assert.Contains("X-PatchPony-Webhook-Token", source, StringComparison.Ordinal);
        Assert.Contains("\"question\": request_text", source, StringComparison.Ordinal);
        Assert.Contains("PatchPony · Knowledge", source, StringComparison.Ordinal);
        Assert.Contains("/knowledge-maintain", source, StringComparison.Ordinal);
        Assert.Contains("knowledge-maintenance", source, StringComparison.Ordinal);
        Assert.Contains("knowledge-question", source, StringComparison.Ordinal);
        Assert.Contains("Bitte beschreibe nach /knowledge-maintain", source, StringComparison.Ordinal);
        Assert.Contains("\"source\": \"open-webui\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"messages\":", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PATCHPONY_AUTH", source, StringComparison.Ordinal);
        Assert.Contains("_render_transparency", source, StringComparison.Ordinal);
        Assert.Contains("verifizierte Tool-Ergebnisse", source, StringComparison.Ordinal);
        Assert.Contains("knowledge.read", source, StringComparison.Ordinal);
        Assert.DoesNotContain("intermediateSteps", source, StringComparison.Ordinal);
        Assert.Contains("asyncio.CancelledError", source, StringComparison.Ordinal);
        Assert.Contains("_upstream_error", source, StringComparison.Ordinal);
        Assert.Contains("status_code == 429", source, StringComparison.Ordinal);
        Assert.DoesNotContain("error.response.text", source, StringComparison.Ordinal);
        Assert.DoesNotContain("knowledge.patch", source, StringComparison.Ordinal);
    }

    [Fact]
    public void N8nArtifact_OnlyOffersReadOnlyRepositoryToolsAndExplicitMaintenanceIntent()
    {
        var root = FindRepositoryRoot();
        var artifact = File.ReadAllText(Path.Combine(root, "integrations", "n8n", "patchpony-read-only-agent.json"));

        Assert.Contains("PatchPony · Knowledge Agent", artifact, StringComparison.Ordinal);
        Assert.Contains("knowledge-question", artifact, StringComparison.Ordinal);
        Assert.Contains("knowledge-maintenance", artifact, StringComparison.Ordinal);
        Assert.Contains("Intent: ", artifact, StringComparison.Ordinal);
        Assert.Contains("projects.list", artifact, StringComparison.Ordinal);
        Assert.Contains("source.search", artifact, StringComparison.Ordinal);
        Assert.Contains("source.read", artifact, StringComparison.Ordinal);
        Assert.DoesNotContain("knowledge.tree", artifact, StringComparison.Ordinal);
        Assert.DoesNotContain("knowledge.search", artifact, StringComparison.Ordinal);
        Assert.DoesNotContain("knowledge.read", artifact, StringComparison.Ordinal);
        Assert.DoesNotContain("knowledge.links", artifact, StringComparison.Ordinal);
        Assert.Contains("Führe niemals Schreiboperationen", artifact, StringComparison.Ordinal);
        Assert.DoesNotContain("knowledge.patch", artifact, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PLAN.md")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the PatchPony repository root.");
    }
}