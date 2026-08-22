using PatchPony.Core.Projects;

namespace PatchPony.Core.Tests;

public sealed class ProjectTests
{
    [Theory]
    [InlineData("file:///tmp/project.git", "main")]
    [InlineData("https://github.com/Edakuban/PatchPony.git", "../main")]
    [InlineData("https://github.com/Edakuban/PatchPony.git", "/main")]
    public void RepositoryRegistration_RejectsLocalRemotesAndUnsafeBranchReferences(string remoteUrl, string branch)
    {
        var result = RepositoryRegistration.Create(ProjectId.New(), new Uri(remoteUrl), branch);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.invalid", result.Error.Code);
    }

    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef01234567")]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    public void RepositoryRevision_AcceptsOnlyImmutableGitObjectIdentifiers(string commitId)
    {
        var result = RepositoryRevision.Create(commitId);

        Assert.True(result.IsSuccess);
        Assert.Equal(commitId, result.Value!.CommitId);
    }

    [Theory]
    [InlineData("main")]
    [InlineData("012345")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void RepositoryRevision_RejectsMutableOrMalformedValues(string commitId)
    {
        var result = RepositoryRevision.Create(commitId);

        Assert.False(result.IsSuccess);
        Assert.Equal("checkout.revision_invalid", result.Error.Code);
    }
}