using System.Text.Json;

namespace PatchPony.IntegrationTests;

public sealed class I6DeveloperQuestionEvaluationTests
{
    [Fact]
    public void EvaluationSet_HasRepresentativeBoundedReadOnlyAndSafetyCases()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "evaluations", "i6-developer-questions.json")));
        var rootElement = document.RootElement;
        var cases = rootElement.GetProperty("cases").EnumerateArray().ToArray();

        Assert.Equal(1, rootElement.GetProperty("schemaVersion").GetInt32());
        Assert.InRange(cases.Length, 10, 20);
        Assert.Equal(cases.Length, cases.Select(item => item.GetProperty("id").GetString()).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(0.8m, rootElement.GetProperty("acceptanceThreshold").GetDecimal());
        Assert.Equal(2, cases.Select(item => item.GetProperty("projectId").GetString()).Distinct(StringComparer.Ordinal).Count());
        Assert.True(cases.Count(item => item.GetProperty("kind").GetString() == "safety-refusal") >= 3);

        foreach (var item in cases)
        {
            Assert.True(new[] { "patchpony", "vocavid" }.Contains(item.GetProperty("projectId").GetString(), StringComparer.Ordinal));
            Assert.True(new[] { "source-answer", "safety-refusal" }.Contains(item.GetProperty("kind").GetString(), StringComparer.Ordinal));
            Assert.InRange(item.GetProperty("question").GetString()!.Length, 1, 12_000);
            Assert.InRange(item.GetProperty("maxToolCalls").GetInt32(), 0, 6);
            foreach (var path in item.GetProperty("expectedCitations").EnumerateArray().Select(value => value.GetString()!))
            {
                Assert.DoesNotContain("..", path, StringComparison.Ordinal);
                Assert.DoesNotContain(".env", path, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(".git", path, StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.All(cases.Where(item => item.GetProperty("kind").GetString() == "safety-refusal"), item =>
        {
            Assert.Equal(0, item.GetProperty("maxToolCalls").GetInt32());
            Assert.Empty(item.GetProperty("expectedCitations").EnumerateArray());
        });
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