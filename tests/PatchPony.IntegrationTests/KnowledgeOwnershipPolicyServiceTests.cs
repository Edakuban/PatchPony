using Microsoft.Extensions.Configuration;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class KnowledgeOwnershipPolicyServiceTests
{
    [Fact]
    public void Resolve_UsesOnlyConfiguredOwnershipEntries()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PatchPony:KnowledgeOwnership:Entries:0:ProjectId"] = "demo-project",
            ["PatchPony:KnowledgeOwnership:Entries:0:PathPattern"] = "docs/**",
            ["PatchPony:KnowledgeOwnership:Entries:0:Owners:0"] = "alice",
            ["PatchPony:KnowledgeOwnership:Entries:0:Reviewers:0"] = "bob"
        }).Build();

        var result = new KnowledgeOwnershipPolicyService(configuration).Resolve("demo-project", "docs/guide.md");

        Assert.True(result.IsSuccess);
        Assert.Equal(["alice"], result.Value!.Owners);
        Assert.Equal(["bob"], result.Value.Reviewers);
    }
}