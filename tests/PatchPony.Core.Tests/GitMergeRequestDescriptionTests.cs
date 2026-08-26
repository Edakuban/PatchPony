using PatchPony.Core.Publication;

namespace PatchPony.Core.Tests;

public sealed class GitMergeRequestDescriptionTests
{
    [Fact]
    public void Create_RendersBoundedStructuredEvidence()
    {
        var result = GitMergeRequestDescription.Create(new MergeRequestDescriptionInput("ZOHO-42", "Update the validated setting.", "One configuration file changed.", [new PublicationTestEvidence("test.billing.unit", "passed")], ["Reviewer should confirm rollout timing."]));
        Assert.True(result.IsSuccess);
        Assert.Contains("Ticket: `ZOHO-42`", result.Value, StringComparison.Ordinal);
        Assert.Contains("## Tests", result.Value, StringComparison.Ordinal);
        Assert.Contains("test.billing.unit", result.Value, StringComparison.Ordinal);
    }
}