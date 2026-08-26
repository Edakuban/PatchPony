using System.Text.RegularExpressions;

namespace PatchPony.Gateway;

public interface IGatewayLogRedactor
{
    string Redact(string value);

    string RedactValue(string fieldName, string? value);

    AccessDecisionAuditEvent Redact(AccessDecisionAuditEvent auditEvent);
}

public sealed partial class GatewayLogRedactor : IGatewayLogRedactor
{
    public const string RedactedValue = "[REDACTED]";

    private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization", "token", "accesstoken", "refreshtoken", "idtoken", "password", "secret", "apikey", "key", "githubtoken", "githubpat", "clientsecret",
        "ticket", "ticketdata", "ticketbody", "ticketdescription", "issue", "issuedata", "issuebody", "issuedescription"
    };

    public string Redact(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var redacted = BearerToken().Replace(value, "Bearer " + RedactedValue);
        return SensitiveAssignment().Replace(redacted, match =>
            match.Groups["key"].Value + match.Groups["separator"].Value + RedactedValue);
    }

    public string RedactValue(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value ?? string.Empty;
        }

        return IsSensitiveFieldName(fieldName) ? RedactedValue : Redact(value);
    }

    public AccessDecisionAuditEvent Redact(AccessDecisionAuditEvent auditEvent) => auditEvent with
    {
        CorrelationId = Redact(auditEvent.CorrelationId),
        Category = Redact(auditEvent.Category),
        Outcome = Redact(auditEvent.Outcome),
        Subject = Redact(auditEvent.Subject),
        AuthenticationMode = Redact(auditEvent.AuthenticationMode),
        Method = Redact(auditEvent.Method),
        Path = Redact(auditEvent.Path),
        ProjectId = auditEvent.ProjectId is null ? null : Redact(auditEvent.ProjectId),
        Tool = auditEvent.Tool is null ? null : Redact(auditEvent.Tool),
        RequiredScope = auditEvent.RequiredScope is null ? null : Redact(auditEvent.RequiredScope)
    };

    private static bool IsSensitiveFieldName(string fieldName) =>
        SensitiveFieldNames.Contains(string.Concat(fieldName.Where(char.IsLetterOrDigit)));

    [GeneratedRegex(@"(?i)\bBearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex BearerToken();

    [GeneratedRegex(@"(?ix)(?<key>""?(?:authorization|access[_-]?token|refresh[_-]?token|id[_-]?token|api[_-]?key|github[_-]?(?:token|pat)|client[_-]?secret|password|secret|token|ticket(?:[_-]?(?:data|body|description))?|issue(?:[_-]?(?:data|body|description))?)""?)(?<separator>\s*(?:=|:)\s*)(?:""[^""]*""|'[^']*'|[^\s,;&}\]]+)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 100)]
    private static partial Regex SensitiveAssignment();
}