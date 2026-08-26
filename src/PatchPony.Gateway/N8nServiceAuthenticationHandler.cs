using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace PatchPony.Gateway;

public sealed class N8nServiceAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string HeaderName = "X-PatchPony-Service-Token";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredToken = configuration[$"{PatchPonyAuthenticationOptions.SectionName}:N8n:Token"];
        var suppliedTokens = Request.Headers[HeaderName];
        if (string.IsNullOrWhiteSpace(configuredToken) || suppliedTokens.Count != 1 || string.IsNullOrWhiteSpace(suppliedTokens[0]))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!TokensMatch(configuredToken, suppliedTokens[0]!))
        {
            return Task.FromResult(AuthenticateResult.Fail("The n8n service token is invalid."));
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "service-n8n"),
                new Claim("sub", "service-n8n"),
                new Claim("client_id", "n8n"),
                new Claim("auth_mode", "service-token"),
                new Claim("roles", PatchPonyRoles.ServiceN8n)
            ],
            Scheme.Name);
        foreach (var project in GetProjects(configuration[$"{PatchPonyAuthenticationOptions.SectionName}:N8n:Projects"]))
        {
            identity.AddClaim(new Claim("project", project));
        }

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool TokensMatch(string configuredToken, string suppliedToken)
    {
        var configured = SHA256.HashData(Encoding.UTF8.GetBytes(configuredToken));
        var supplied = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedToken));
        return CryptographicOperations.FixedTimeEquals(configured, supplied);
    }

    private static IEnumerable<string> GetProjects(string? configuredProjects) =>
        (configuredProjects ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}