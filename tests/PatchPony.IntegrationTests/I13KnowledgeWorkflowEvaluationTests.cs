using System.Text.Json;

namespace PatchPony.IntegrationTests;

public sealed class I13KnowledgeWorkflowEvaluationTests
{
    [Fact]
    public void EvaluationSet_IsVersionedTemplateOnlyAndCoversQuestionsProposalsAndSafetyBoundaries()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "evaluations", "i13-knowledge-workflow.json")));
        var evaluation = document.RootElement;
        var cases = evaluation.GetProperty("cases").EnumerateArray().ToArray();

        Assert.Equal(1, evaluation.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("pending-pilot-sample-selection", evaluation.GetProperty("status").GetString());
        Assert.True(evaluation.GetProperty("prerequisites").GetArrayLength() >= 5);
        Assert.Equal(12, cases.Length);
        Assert.True(cases.Count(item => item.GetProperty("kind").GetString() == "knowledge-question") >= 4);
        Assert.True(cases.Count(item => item.GetProperty("kind").GetString() == "knowledge-maintenance") >= 3);
        Assert.True(cases.Count(item => item.GetProperty("kind").GetString() == "safety-refusal") >= 5);

        Assert.All(cases.Where(item => item.GetProperty("kind").GetString() == "knowledge-maintenance"), item =>
        {
            Assert.StartsWith("/knowledge-maintain ", item.GetProperty("requestTemplate").GetString(), StringComparison.Ordinal);
            Assert.NotEmpty(item.GetProperty("expectedProposalOperations").EnumerateArray());
        });
        Assert.All(cases.Where(item => item.GetProperty("kind").GetString() == "safety-refusal"), item =>
        {
            Assert.Empty(item.GetProperty("expectedTools").EnumerateArray());
            Assert.Empty(item.GetProperty("expectedProposalOperations").EnumerateArray());
        });

        var source = document.RootElement.GetRawText();
        Assert.DoesNotContain("https://", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BEGIN", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("knowledge.patch", source, StringComparison.Ordinal);
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