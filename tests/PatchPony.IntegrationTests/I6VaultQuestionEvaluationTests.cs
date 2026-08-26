using System.Text.Json;

namespace PatchPony.IntegrationTests;

public sealed class I6VaultQuestionEvaluationTests
{
    [Fact]
    public void VaultEvaluationSet_IsPreparedButCannotBeRunWithoutARegisteredVault()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "evaluations", "i6-vault-questions.json")));
        var evaluation = document.RootElement;
        var cases = evaluation.GetProperty("cases").EnumerateArray().ToArray();

        Assert.Equal(1, evaluation.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("pending-vault-registration", evaluation.GetProperty("status").GetString());
        Assert.Equal(8, cases.Length);
        Assert.True(evaluation.GetProperty("prerequisites").GetArrayLength() >= 4);
        Assert.True(cases.Count(item => item.GetProperty("kind").GetString() == "knowledge-answer") >= 5);
        Assert.True(cases.Count(item => item.GetProperty("kind").GetString() == "safety-refusal") >= 3);
        Assert.All(cases.Where(item => item.GetProperty("kind").GetString() == "safety-refusal"), item =>
        {
            Assert.Empty(item.GetProperty("expectedTools").EnumerateArray());
            Assert.Equal("none", item.GetProperty("expectedCitationKind").GetString());
        });
        Assert.Contains(cases.Where(item => item.GetProperty("kind").GetString() == "knowledge-answer"), item =>
            item.GetProperty("expectedTools").EnumerateArray().Select(tool => tool.GetString()).Contains("knowledge.search"));
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