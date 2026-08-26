namespace PatchPony.Gateway;

public sealed class PatchPonyAuthenticationOptions
{
    public const string SectionName = "PatchPony:Auth";

    public OidcAuthenticationOptions Oidc { get; init; } = new();

    public string? DevelopmentPassword { get; init; }

    public N8nServiceAuthenticationOptions N8n { get; init; } = new();
}

public sealed class OidcAuthenticationOptions
{
    public string? Authority { get; init; }

    public string? Audience { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;
}

public sealed class N8nServiceAuthenticationOptions
{
    public string? Token { get; init; }
}

public static class PatchPonyAuthenticationDefaults
{
    public const string Scheme = "PatchPony";
    public const string DevelopmentPasswordScheme = "DevelopmentPassword";
    public const string N8nServiceScheme = "N8nService";
}