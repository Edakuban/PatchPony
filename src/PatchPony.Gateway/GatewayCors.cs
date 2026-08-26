using Microsoft.AspNetCore.Cors.Infrastructure;

namespace PatchPony.Gateway;

public sealed class GatewayCorsOptions
{
    public const string SectionName = "PatchPony:Cors";

    public string[] AllowedOrigins { get; init; } = [];

    public void Validate()
    {
        foreach (var origin in AllowedOrigins)
        {
            if (string.IsNullOrWhiteSpace(origin)
                || !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || !string.IsNullOrEmpty(uri.UserInfo)
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment)
                || uri.AbsolutePath != "/"
                || !string.Equals(uri.GetLeftPart(UriPartial.Authority), origin, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"CORS origin '{origin}' must be an exact HTTP(S) origin without a path, query, fragment, credentials, or trailing slash.");
            }
        }
    }
}

public static class GatewayCors
{
    public const string PolicyName = "patchpony";

    public static void Configure(CorsOptions options, GatewayCorsOptions cors)
    {
        var allowedOrigins = cors.AllowedOrigins.ToHashSet(StringComparer.Ordinal);

        options.AddPolicy(PolicyName, policy => policy
            .SetIsOriginAllowed(allowedOrigins.Contains)
            .WithMethods("GET", "POST")
            .WithHeaders(
                "Authorization",
                "Content-Type",
                CorrelationMiddleware.HeaderName,
                DevelopmentPasswordAuthenticationHandler.HeaderName,
                N8nServiceAuthenticationHandler.HeaderName)
            .WithExposedHeaders(CorrelationMiddleware.HeaderName)
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
    }
}