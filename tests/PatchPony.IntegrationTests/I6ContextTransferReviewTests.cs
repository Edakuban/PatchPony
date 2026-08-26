using System.Text.Json;

namespace PatchPony.IntegrationTests;

public sealed class I6ContextTransferReviewTests
{
    [Fact]
    public void ContextTransferInventory_DescribesOnlyTheReadOnlyPilotAndKeepsFormalApprovalOpen()
    {
        var root = FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "evaluations", "i6-context-transfer.json")));
        var review = document.RootElement;
        var transfers = review.GetProperty("transfers").EnumerateArray().ToArray();

        Assert.Equal(1, review.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("completed-formal-approval-pending", review.GetProperty("technicalReviewStatus").GetString());
        Assert.Equal(5, transfers.Length);
        Assert.Contains(transfers, transfer => transfer.GetProperty("to").GetString() == "destination.one model provider");
        Assert.Contains(transfers, transfer => transfer.GetProperty("excluded").EnumerateArray().Select(value => value.GetString()).Contains(".env"));
        Assert.Contains(transfers, transfer => transfer.GetProperty("excluded").EnumerateArray().Select(value => value.GetString()).Contains("full chat history"));
        Assert.True(review.GetProperty("releaseConditions").GetArrayLength() >= 4);
        Assert.Contains(review.GetProperty("releaseConditions").EnumerateArray().Select(value => value.GetString()), value => value!.Contains("Keycloak/OIDC", StringComparison.Ordinal));
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