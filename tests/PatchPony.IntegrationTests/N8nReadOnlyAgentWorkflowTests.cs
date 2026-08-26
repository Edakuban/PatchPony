using System.Text.Json;

namespace PatchPony.IntegrationTests;

public sealed class N8nReadOnlyAgentWorkflowTests
{
    [Fact]
    public void Workflow_IsInactiveAndContainsOnlyTheReadOnlyKnowledgeAgentPath()
    {
        var root = FindRepositoryRoot();
        using var workflow = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "integrations", "n8n", "patchpony-read-only-agent.json")));
        var source = workflow.RootElement.GetRawText();
        var nodes = workflow.RootElement.GetProperty("nodes").EnumerateArray().ToArray();

        Assert.False(workflow.RootElement.GetProperty("active").GetBoolean());
        Assert.Contains(nodes, node => node.GetProperty("name").GetString() == "Read-only PatchPony Agent");
        var mcp = Assert.Single(nodes, node => node.GetProperty("name").GetString() == "PatchPony read-only MCP");
        var webhook = Assert.Single(nodes, node => node.GetProperty("name").GetString() == "Open WebUI webhook");
        var validation = Assert.Single(nodes, node => node.GetProperty("name").GetString() == "Validate minimal request");
        Assert.Equal("headerAuth", webhook.GetProperty("parameters").GetProperty("authentication").GetString());
        Assert.Contains("validProject", validation.GetProperty("parameters").GetProperty("jsCode").GetString(), StringComparison.Ordinal);
        Assert.Contains("owui-sha256", validation.GetProperty("parameters").GetProperty("jsCode").GetString(), StringComparison.Ordinal);
        Assert.Contains("knowledge-maintenance", validation.GetProperty("parameters").GetProperty("jsCode").GetString(), StringComparison.Ordinal);
        Assert.Equal("httpStreamable", mcp.GetProperty("parameters").GetProperty("serverTransport").GetString());
        Assert.Equal(["runtime.status", "runtime.validate_correlation", "projects.list", "source.search", "source.read"], mcp.GetProperty("parameters").GetProperty("includeTools").EnumerateArray().Select(tool => tool.GetString()));
        var agent = Assert.Single(nodes, node => node.GetProperty("name").GetString() == "Read-only PatchPony Agent");
        Assert.Contains("[projekt:pfad:Lzeile]", agent.GetProperty("parameters").GetProperty("options").GetProperty("systemMessage").GetString(), StringComparison.Ordinal);
        Assert.Contains("knowledge-maintenance", agent.GetProperty("parameters").GetProperty("options").GetProperty("systemMessage").GetString(), StringComparison.Ordinal);
        Assert.Contains("projects.list", agent.GetProperty("parameters").GetProperty("options").GetProperty("systemMessage").GetString(), StringComparison.Ordinal);
        Assert.Contains("keine Recherchegrenze", agent.GetProperty("parameters").GetProperty("options").GetProperty("systemMessage").GetString(), StringComparison.Ordinal);
        Assert.Contains("keine Knowledge-Vault-Tools", agent.GetProperty("parameters").GetProperty("options").GetProperty("systemMessage").GetString(), StringComparison.Ordinal);
        Assert.Contains("Initial project ID", agent.GetProperty("parameters").GetProperty("text").GetString(), StringComparison.Ordinal);
        Assert.Contains("Intent: ", agent.GetProperty("parameters").GetProperty("text").GetString(), StringComparison.Ordinal);
        Assert.True(agent.GetProperty("parameters").GetProperty("options").GetProperty("returnIntermediateSteps").GetBoolean());
        Assert.Equal(6, agent.GetProperty("parameters").GetProperty("options").GetProperty("maxIterations").GetInt32());
        Assert.Equal("continueRegularOutput", agent.GetProperty("onError").GetString());
        var formatter = Assert.Single(nodes, node => node.GetProperty("name").GetString() == "Format safe response");
        Assert.Contains("intermediateSteps", formatter.GetProperty("parameters").GetProperty("jsCode").GetString(), StringComparison.Ordinal);
        Assert.Contains("sources", formatter.GetProperty("parameters").GetProperty("jsCode").GetString(), StringComparison.Ordinal);
        Assert.Contains("toolCalls.length < 6", formatter.GetProperty("parameters").GetProperty("jsCode").GetString(), StringComparison.Ordinal);
        Assert.Contains("safeFailure", formatter.GetProperty("parameters").GetProperty("jsCode").GetString(), StringComparison.Ordinal);
        Assert.Contains("max iterations", formatter.GetProperty("parameters").GetProperty("jsCode").GetString(), StringComparison.Ordinal);
        Assert.DoesNotContain("errorMessage }", formatter.GetProperty("parameters").GetProperty("jsCode").GetString(), StringComparison.Ordinal);
        var response = Assert.Single(nodes, node => node.GetProperty("name").GetString() == "Respond to Open WebUI");
        Assert.Contains("toolCalls", response.GetProperty("parameters").GetProperty("responseBody").GetString(), StringComparison.Ordinal);
        Assert.Contains("X-PatchPony-Webhook-Token", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Execute Command", source, StringComparison.Ordinal);
        Assert.DoesNotContain("knowledge.", mcp.GetProperty("parameters").GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("write", mcp.GetProperty("parameters").GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PATCHPONY_N8N_SERVICE_TOKEN", source, StringComparison.Ordinal);
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