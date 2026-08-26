using Microsoft.Extensions.Logging.Abstractions;
using PatchPony.Gateway;

namespace PatchPony.IntegrationTests;

public sealed class GatewayLogRedactorTests
{
    private readonly GatewayLogRedactor redactor = new();

    [Theory]
    [InlineData("Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.payload.signature")]
    [InlineData("token=service-token-value")]
    [InlineData("{\"apiKey\":\"provider-secret\",\"query\":\"safe\"}")]
    [InlineData("ticketDescription=Customer contact and incident details")]
    [InlineData("password=local-development-password")]
    [InlineData("GitHub_Token=github_pat_ROTATED_DO_NOT_LOG_01234567890")]
    [InlineData("client_secret=rotated-oauth-secret")]
    public void Redact_RemovesSensitiveValues(string message)
    {
        var result = redactor.Redact(message);

        Assert.Contains(GatewayLogRedactor.RedactedValue, result, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiJ9", result, StringComparison.Ordinal);
        Assert.DoesNotContain("service-token-value", result, StringComparison.Ordinal);
        Assert.DoesNotContain("provider-secret", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Customer contact", result, StringComparison.Ordinal);
        Assert.DoesNotContain("local-development-password", result, StringComparison.Ordinal);
        Assert.DoesNotContain("github_pat_ROTATED_DO_NOT_LOG_01234567890", result, StringComparison.Ordinal);
        Assert.DoesNotContain("rotated-oauth-secret", result, StringComparison.Ordinal);
    }

    [Fact]
    public void RedactValue_MasksKnownSensitiveStructuredFields()
    {
        Assert.Equal(GatewayLogRedactor.RedactedValue, redactor.RedactValue("refresh_token", "refresh-value"));
        Assert.Equal("read-only", redactor.RedactValue("mode", "read-only"));
    }

    [Fact]
    public void AccessDecisionAudit_RetainsOnlyRedactedData()
    {
        var audit = new AccessDecisionAudit(NullLogger<AccessDecisionAudit>.Instance, redactor);
        audit.Record(new AccessDecisionAuditEvent(
            DateTimeOffset.UtcNow,
            "correlation-42",
            "policy",
            "rejected",
            "Bearer subject-token",
            "service-token",
            "POST",
            "/api/v1/projects/demo/access?ticketDescription=private-ticket-data",
            "demo",
            "source.read",
            "source.read"));

        var stored = Assert.Single(audit.GetRecent(1));
        Assert.Contains(GatewayLogRedactor.RedactedValue, stored.Subject, StringComparison.Ordinal);
        Assert.Contains(GatewayLogRedactor.RedactedValue, stored.Path, StringComparison.Ordinal);
        Assert.DoesNotContain("subject-token", stored.Subject, StringComparison.Ordinal);
        Assert.DoesNotContain("private-ticket-data", stored.Path, StringComparison.Ordinal);
    }
}