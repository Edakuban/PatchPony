using System.Text.Json;

namespace PatchPony.IntegrationTests;

public sealed class I12ChangeRequestEvaluationTests
{
    [Fact]
    public void EvaluationSet_IsPrivacyPreservingAndCoversAllPolicyOutcomes()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "evaluations", "i12-change-requests.json")));
        var evaluation = document.RootElement;
        var cases = evaluation.GetProperty("cases").EnumerateArray().ToArray();
        var source = evaluation.GetRawText();

        Assert.Equal(1, evaluation.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("pending-historical-export", evaluation.GetProperty("status").GetString());
        Assert.Equal(9, cases.Length);
        Assert.Contains(cases, item => item.GetProperty("category").GetString() == "production-approval");
        Assert.Contains(cases, item => item.GetProperty("category").GetString() == "conflicting-values");
        Assert.Contains(cases, item => item.GetProperty("category").GetString() == "feature-source");
        Assert.Contains(cases, item => item.GetProperty("category").GetString() == "secret-attempt");
        Assert.DoesNotContain("access-token", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "PLAN.md"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the PatchPony repository root.");
    }
}