using PatchPony.Infrastructure.Publication;

namespace PatchPony.IntegrationTests;

public sealed class GitHubProviderOptionsTests
{
    [Fact]
    public void CreateCredentials_RequiresOneGitHubRepositoryAndSecretLikeToken()
    {
        var credentials = new GitHubProviderOptions
        {
            RepositoryUri = "https://github.com/Edakuban/PatchPony",
            Token = "github_pat_012345678901234567890123456789"
        }.CreateCredentials();

        Assert.True(credentials.Matches(new Uri("https://github.com/edakuban/patchpony")));
        Assert.Equal("Bearer", credentials.CreateAuthorizationHeader().Scheme);
        Assert.Throws<InvalidOperationException>(() => new GitHubProviderOptions { RepositoryUri = "https://gitlab.com/group/repo", Token = "short" }.CreateCredentials());
    }

    [Fact]
    public void CredentialStore_RotatesAtomicallyWithoutChangingRepositoryBinding()
    {
        var repository = new Uri("https://github.com/Edakuban/PatchPony");
        var initial = new GitHubProviderCredentials(repository, "github_pat_initial_012345678901234567890");
        var store = new GitHubProviderCredentialStore(initial);
        var replacement = new GitHubProviderCredentials(repository, "github_pat_rotated_012345678901234567890");

        store.Rotate(replacement);

        Assert.Same(replacement, store.GetSnapshot());
        Assert.Throws<InvalidOperationException>(() => store.Rotate(new GitHubProviderCredentials(new Uri("https://github.com/Edakuban/Other"), "github_pat_other_012345678901234567890")));
        Assert.Same(replacement, store.GetSnapshot());
    }}