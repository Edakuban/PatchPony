using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace PatchPony.Gateway;

public sealed class DevelopmentPasswordAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHostEnvironment environment,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string HeaderName = "X-PatchPony-Development-Password";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment())
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var configuredPassword = configuration[$"{PatchPonyAuthenticationOptions.SectionName}:DevelopmentPassword"];
        var suppliedPassword = Request.Headers[HeaderName].SingleOrDefault();
        if (string.IsNullOrWhiteSpace(configuredPassword) || string.IsNullOrWhiteSpace(suppliedPassword))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!PasswordsMatch(configuredPassword, suppliedPassword))
        {
            return Task.FromResult(AuthenticateResult.Fail("The development password is invalid."));
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "local-developer"),
                new Claim("sub", "local-developer"),
                new Claim("auth_mode", "development-password"),
                new Claim("roles", PatchPonyRoles.CodeReader),
                new Claim("roles", PatchPonyRoles.IssuePlanner),
                new Claim("roles", PatchPonyRoles.KnowledgeReader),
                new Claim("roles", PatchPonyRoles.Reviewer)
            ],
            Scheme.Name);
        foreach (var project in GetProjects(configuration[$"{PatchPonyAuthenticationOptions.SectionName}:DevelopmentProjects"]))
        {
            identity.AddClaim(new Claim("project", project));
        }
        foreach (var project in GetProjects(configuration[$"{PatchPonyAuthenticationOptions.SectionName}:DevelopmentConfigEditorProjects"]))
        {
            identity.AddClaim(new Claim("project_role", $"{project}:{PatchPonyRoles.ConfigEditor}"));
        }

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool PasswordsMatch(string configuredPassword, string suppliedPassword)
    {
        var configured = Encoding.UTF8.GetBytes(configuredPassword);
        var supplied = Encoding.UTF8.GetBytes(suppliedPassword);
        return CryptographicOperations.FixedTimeEquals(configured, supplied);
    }
    private static IEnumerable<string> GetProjects(string? configuredProjects) =>
        (configuredProjects ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}