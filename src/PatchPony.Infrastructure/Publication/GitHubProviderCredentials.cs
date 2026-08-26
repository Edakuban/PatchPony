using System.Net.Http.Headers;

namespace PatchPony.Infrastructure.Publication;

/// <summary>Secret-bearing GitHub credential bound to one server-configured repository. Never log this object or its token.</summary>
public sealed class GitHubProviderCredentials
{
    public GitHubProviderCredentials(Uri repositoryUri, string token)
    {
        ArgumentNullException.ThrowIfNull(repositoryUri);
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("A GitHub token is required.", nameof(token));
        RepositoryUri = repositoryUri;
        Token = token;
    }

    public Uri RepositoryUri { get; }
    public string Token { get; }

    public bool Matches(Uri repositoryUri) => Uri.Compare(RepositoryUri, repositoryUri, UriComponents.Host | UriComponents.Path, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;
    public AuthenticationHeaderValue CreateAuthorizationHeader() => new("Bearer", Token);
}

/// <summary>Atomically replaces a repository-bound credential; each provider call uses one immutable snapshot.</summary>
public sealed class GitHubProviderCredentialStore
{
    private GitHubProviderCredentials current;

    public GitHubProviderCredentialStore(GitHubProviderCredentials initial) => current = initial ?? throw new ArgumentNullException(nameof(initial));

    public GitHubProviderCredentials GetSnapshot() => Volatile.Read(ref current);

    public void Rotate(GitHubProviderCredentials replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        var previous = GetSnapshot();
        if (!previous.Matches(replacement.RepositoryUri))
            throw new InvalidOperationException("GitHub credential rotation cannot change the bound repository.");
        Interlocked.Exchange(ref current, replacement);
    }
}

public sealed class GitHubProviderOptions
{
    public const string SectionName = "PatchPony:GitHub";
    public string RepositoryUri { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;

    public GitHubProviderCredentials CreateCredentials()
    {
        if (!Uri.TryCreate(RepositoryUri, UriKind.Absolute, out var repository) || repository.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(repository.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
            repository.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).Length != 2 ||
            string.IsNullOrWhiteSpace(Token) || Token.Length < 20 || Token.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("GitHub credentials require one HTTPS github.com repository and a non-empty secret token.");
        }

        return new GitHubProviderCredentials(repository, Token);
    }
}