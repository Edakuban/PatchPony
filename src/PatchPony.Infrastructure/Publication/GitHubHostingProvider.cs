using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PatchPony.Core.Common;
using PatchPony.Core.Publication;

namespace PatchPony.Infrastructure.Publication;

/// <summary>GitHub REST adapter. Authentication is deliberately configured separately in I10.3.</summary>
public sealed class GitHubHostingProvider : IGitHostingProvider
{
    private readonly HttpClient client;
    private readonly GitHubProviderCredentialStore credentials;

    public GitHubHostingProvider(HttpClient client, GitHubProviderCredentials credentials) : this(client, new GitHubProviderCredentialStore(credentials)) { }

    public GitHubHostingProvider(HttpClient client, GitHubProviderCredentialStore credentials)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
    }

    public GitHostingProviderKind Kind => GitHostingProviderKind.GitHub;

    public async Task<Result<GitMergeRequestReference?>> FindOpenAsync(GitMergeRequestDraft draft, CancellationToken cancellationToken = default)
    {
        var valid = draft.Validate();
        if (!valid.IsSuccess) return Result<GitMergeRequestReference?>.Failure(valid.Error);
        var credential = credentials.GetSnapshot();
        if (!credential.Matches(draft.RepositoryUri) || !TryRepository(draft.RepositoryUri, out var owner, out var repository)) return UnsupportedRepository<GitMergeRequestReference?>();

        try
        {
            using var request = CreateRequest(credential, HttpMethod.Get, $"repos/{owner}/{repository}/pulls?state=open&head={Uri.EscapeDataString($"{owner}:{draft.SourceBranch}")}&base={Uri.EscapeDataString(draft.TargetBranch)}");
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return ProviderFailure<GitMergeRequestReference?>();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false));
            var first = document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0 ? document.RootElement[0] : default;
            return first.ValueKind == JsonValueKind.Undefined ? Result<GitMergeRequestReference?>.Success(null) : Result<GitMergeRequestReference?>.Success(ToReference(first, draft));
        }
        catch (HttpRequestException) { return ProviderFailure<GitMergeRequestReference?>(); }
        catch (JsonException) { return ProviderFailure<GitMergeRequestReference?>(); }
    }

    public async Task<Result<GitMergeRequestReference>> CreateAsync(GitMergeRequestDraft draft, CancellationToken cancellationToken = default)
    {
        var valid = draft.Validate();
        if (!valid.IsSuccess) return Result<GitMergeRequestReference>.Failure(valid.Error);
        var credential = credentials.GetSnapshot();
        if (!credential.Matches(draft.RepositoryUri) || !TryRepository(draft.RepositoryUri, out var owner, out var repository)) return UnsupportedRepository<GitMergeRequestReference>();

        try
        {
            var body = JsonSerializer.Serialize(new { title = draft.Title, head = draft.SourceBranch, @base = draft.TargetBranch, body = draft.Description });
            using var request = CreateRequest(credential, HttpMethod.Post, $"repos/{owner}/{repository}/pulls");
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Created) return ProviderFailure<GitMergeRequestReference>();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false));
            return Result<GitMergeRequestReference>.Success(ToReference(document.RootElement, draft));
        }
        catch (HttpRequestException) { return ProviderFailure<GitMergeRequestReference>(); }
        catch (JsonException) { return ProviderFailure<GitMergeRequestReference>(); }
    }

    public async Task<Result> RequestReviewersAsync(GitMergeRequestDraft draft, GitMergeRequestReference mergeRequest, IReadOnlyList<string> reviewers, CancellationToken cancellationToken = default)
    {
        var valid = draft.Validate();
        var credential = credentials.GetSnapshot();
        if (!valid.IsSuccess || mergeRequest.Provider != GitHostingProviderKind.GitHub || !int.TryParse(mergeRequest.ProviderId, out var number) || reviewers.Count == 0 || !credential.Matches(draft.RepositoryUri) || !TryRepository(draft.RepositoryUri, out var owner, out var repository)) return Result.Failure(new DomainError("git_provider.reviewers_invalid", "The GitHub reviewer assignment is invalid."));
        try
        {
            using var request = CreateRequest(credential, HttpMethod.Post, $"repos/{owner}/{repository}/pulls/{number}/requested_reviewers");
            request.Content = new StringContent(JsonSerializer.Serialize(new { reviewers }), Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(new DomainError("git_provider.reviewers_failed", "The GitHub reviewer assignment failed."));
        }
        catch (HttpRequestException) { return Result.Failure(new DomainError("git_provider.reviewers_failed", "The GitHub reviewer assignment failed.")); }
    }
    private HttpRequestMessage CreateRequest(GitHubProviderCredentials credential, HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, new Uri(client.BaseAddress ?? new Uri("https://api.github.com/"), path));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Authorization = credential.CreateAuthorizationHeader();
        request.Headers.UserAgent.ParseAdd("PatchPony/1.0");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private static bool TryRepository(Uri uri, out string owner, out string repository)
    {
        owner = repository = string.Empty;
        var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) || parts.Length != 2 || parts.Any(part => part.Length is 0 or > 100 || part.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_' or '.')))) return false;
        owner = parts[0]; repository = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? parts[1][..^4] : parts[1];
        return repository.Length > 0;
    }

    private static GitMergeRequestReference ToReference(JsonElement item, GitMergeRequestDraft draft) => new(
        GitHostingProviderKind.GitHub,
        item.GetProperty("number").GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture),
        new Uri(item.GetProperty("html_url").GetString()!, UriKind.Absolute),
        draft.SourceBranch,
        draft.TargetBranch);
    private static Result<T> UnsupportedRepository<T>() => Result<T>.Failure(new DomainError("git_provider.repository_unsupported", "The repository is not a supported GitHub repository."));
    private static Result<T> ProviderFailure<T>() => Result<T>.Failure(new DomainError("git_provider.request_failed", "The Git hosting provider request failed."));
}