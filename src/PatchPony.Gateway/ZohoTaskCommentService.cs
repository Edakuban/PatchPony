using System.Net.Http.Headers;
using PatchPony.Core.Common;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ZohoTaskCommentOptions
{
    public string? ApiBaseUri { get; init; }
    public string? PortalId { get; init; }
    public string? AccessToken { get; init; }
}

/// <summary>Posts already validated outcome comments to an explicitly configured Zoho task.</summary>
public sealed class ZohoTaskCommentService(HttpClient client, IConfiguration configuration)
{
    public async Task<Result> PostAsync(string zohoProjectId, string taskId, TicketComment comment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);
        var options = configuration.GetSection(ZohoWebhookOptions.SectionName).Get<ZohoTaskCommentOptions>() ?? new ZohoTaskCommentOptions();
        if (!IsConfigured(options) || !IsIdentifier(zohoProjectId) || !IsIdentifier(taskId))
            return Result.Failure(new DomainError("zoho.provider.unconfigured", "The Zoho task comment provider is not configured."));

        var baseUri = options.ApiBaseUri!.TrimEnd('/');
        var requestUri = $"{baseUri}/restapi/portal/{Uri.EscapeDataString(options.PortalId!)}/projects/{Uri.EscapeDataString(zohoProjectId)}/tasks/{Uri.EscapeDataString(taskId)}/comments/";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["content"] = comment.RenderMarkdown() })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(new DomainError("zoho.provider.request_failed", "The Zoho task comment request failed."));
        }
        catch (HttpRequestException)
        {
            return Result.Failure(new DomainError("zoho.provider.request_failed", "The Zoho task comment request failed."));
        }
    }

    private static bool IsConfigured(ZohoTaskCommentOptions options) =>
        Uri.TryCreate(options.ApiBaseUri, UriKind.Absolute, out var apiBase) && apiBase.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(apiBase.UserInfo) &&
        IsIdentifier(options.PortalId) && !string.IsNullOrWhiteSpace(options.AccessToken) && options.AccessToken.Length <= 4_096 && !options.AccessToken.Any(char.IsControl);
    private static bool IsIdentifier(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
}