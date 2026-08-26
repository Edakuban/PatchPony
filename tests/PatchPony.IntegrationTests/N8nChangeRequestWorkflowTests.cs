using System.Text.Json;

namespace PatchPony.IntegrationTests;

public sealed class N8nChangeRequestWorkflowTests
{
    [Fact]
    public void Workflow_IsInactiveAndValidatesOnlyChangeRequestConfigIntake()
    {
        var root = FindRepositoryRoot();
        using var workflow = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "integrations", "n8n", "patchpony-change-request.json")));
        var nodes = workflow.RootElement.GetProperty("nodes").EnumerateArray().ToArray();

        Assert.False(workflow.RootElement.GetProperty("active").GetBoolean());
        var webhook = Assert.Single(nodes, node => node.GetProperty("name").GetString() == "Change request webhook");
        Assert.Equal("headerAuth", webhook.GetProperty("parameters").GetProperty("authentication").GetString());
        var validation = Assert.Single(nodes, node => node.GetProperty("name").GetString() == "Validate change request envelope");
        var code = validation.GetProperty("parameters").GetProperty("jsCode").GetString();
        Assert.Contains("change_request", code, StringComparison.Ordinal);
        Assert.Contains("Configuration", code, StringComparison.Ordinal);
        Assert.DoesNotContain("PATCHPONY_N8N_SERVICE_TOKEN", workflow.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("Execute Command", workflow.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "PLAN.md"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the PatchPony repository root.");
    }
}