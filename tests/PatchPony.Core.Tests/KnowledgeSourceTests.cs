using PatchPony.Core.Knowledge;
using PatchPony.Core.Projects;

namespace PatchPony.Core.Tests;

public sealed class KnowledgeSourceTests
{
    [Theory]
    [InlineData("Vault")]
    [InlineData("../vault")]
    [InlineData("vault/child")]
    public void Create_RejectsUnsafeSourceNames(string name)
    {
        var result = KnowledgeSource.Create(KnowledgeSourceId.New(), ProjectId.New(), name, new Uri("https://github.com/example/vault.git"), "main", DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid", result.Error.Code);
    }

    [Theory]
    [InlineData("file:///server/vault.git", "main")]
    [InlineData("https://github.com/example/vault.git", "../main")]
    public void Create_RejectsUnsafeRemoteOrBranch(string remoteUrl, string branch)
    {
        var result = KnowledgeSource.Create(KnowledgeSourceId.New(), ProjectId.New(), "vault", new Uri(remoteUrl), branch, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid", result.Error.Code);
    }
}
