using System.Security.Cryptography;
using System.Text;
using PatchPony.Core.Common;

namespace PatchPony.Core.Workflows;

/// <summary>Canonical, content-free identity for one Zoho task event.</summary>
public static class ZohoWebhookIdempotencyKey
{
    public static Result<string> Create(string eventType, string ticketId, string revision)
    {
        if (!IsEventType(eventType) || !IsIdentifier(ticketId) || !IsIdentifier(revision))
            return Result<string>.Failure(new DomainError("zoho.idempotency.invalid", "The Zoho event identity is invalid."));

        var canonical = $"zoho-v1|{eventType.Length}:{eventType}|{ticketId.Length}:{ticketId}|{revision.Length}:{revision}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Result<string>.Success("zoho:v1:" + Convert.ToHexStringLower(digest));
    }

    private static bool IsEventType(string value) => value is "task.created" or "task.updated";
    private static bool IsIdentifier(string value) => !string.IsNullOrEmpty(value) && value.Length <= 128 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
}