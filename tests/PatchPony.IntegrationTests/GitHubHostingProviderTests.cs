using System.Net;
using System.Text;
using PatchPony.Core.Jobs;
using PatchPony.Core.Projects;
using PatchPony.Core.Publication;
using PatchPony.Core.Sessions;
using PatchPony.Infrastructure.Publication;

namespace PatchPony.IntegrationTests;

public sealed class GitHubHostingProviderTests
{
    [Fact]
    public async Task FindAndCreate_UseOnlyTheValidatedGitHubRepositoryAndDraftFields()
    {
        var handler = new RecordingHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        var provider = new GitHubHostingProvider(client, new GitHubProviderCredentials(new Uri("https://github.com/Edakuban/PatchPony"), "github_pat_012345678901234567890123456789"));
        var draft = new GitMergeRequestDraft(ProjectId.New(), JobId.New(), SessionId.New(), new Uri("https://github.com/Edakuban/PatchPony"), "patchpony/session/abc", "main", "Patch", "Validated.");

        var existing = await provider.FindOpenAsync(draft);
        var created = await provider.CreateAsync(draft);

        Assert.True(existing.IsSuccess);
        Assert.Null(existing.Value);
        Assert.True(created.IsSuccess);
        Assert.Equal("42", created.Value!.ProviderId);
        Assert.Equal(["GET /repos/Edakuban/PatchPony/pulls?state=open&head=Edakuban%3Apatchpony%2Fsession%2Fabc&base=main", "POST /repos/Edakuban/PatchPony/pulls"], handler.Requests);
        Assert.Contains("\"head\":\"patchpony/session/abc\"", handler.PostBody, StringComparison.Ordinal);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];
        public string PostBody { get; private set; } = string.Empty;
        public string? AuthorizationScheme { get; private set; }
        public List<string?> AuthorizationParameters { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameters.Add(request.Headers.Authorization?.Parameter);
            if (request.Method == HttpMethod.Get) return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]") };
            PostBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{\"number\":42,\"html_url\":\"https://github.com/Edakuban/PatchPony/pull/42\"}", Encoding.UTF8, "application/json") };
        }
    }
}