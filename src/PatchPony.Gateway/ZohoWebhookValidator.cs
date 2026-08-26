using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PatchPony.Core.Workflows;

namespace PatchPony.Gateway;

public sealed class ZohoWebhookOptions
{
    public const string SectionName = "PatchPony:Zoho";
    public string? WebhookSecret { get; init; }
}

public sealed record ZohoAttachment(string Id, string FileName, string ContentType, long SizeBytes);
public sealed record ZohoTaskWebhook(string EventType, string TaskId, string Revision, string ZohoProjectId, string Channel, string Title, string Description, IReadOnlyList<ZohoAttachment> Attachments);

public sealed record ZohoWebhookValidationResult(bool IsValid, ZohoTaskWebhook? Payload, string? IdempotencyKey, string? ErrorCode)
{
    public static ZohoWebhookValidationResult Invalid(string errorCode) => new(false, null, null, errorCode);
    public static ZohoWebhookValidationResult Valid(ZohoTaskWebhook payload, string idempotencyKey) => new(true, payload, idempotencyKey, null);
}

/// <summary>Fail-closed signature and schema boundary for incoming Zoho task events.</summary>
public sealed class ZohoWebhookValidator(IConfiguration configuration)
{
    public const string SignatureHeaderName = "X-PatchPony-Zoho-Signature";
    public const int MaximumPayloadBytes = 64 * 1024;

    public async Task<ZohoWebhookValidationResult> ValidateAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        var secret = configuration[$"{ZohoWebhookOptions.SectionName}:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32) return ZohoWebhookValidationResult.Invalid("zoho.webhook.unconfigured");
        if (!HttpMethods.IsPost(request.Method) || request.ContentLength is > MaximumPayloadBytes) return ZohoWebhookValidationResult.Invalid("zoho.webhook.payload_invalid");
        var signatures = request.Headers[SignatureHeaderName];
        if (signatures.Count != 1 || string.IsNullOrWhiteSpace(signatures[0])) return ZohoWebhookValidationResult.Invalid("zoho.webhook.signature_invalid");

        byte[] body;
        try { body = await ReadBoundedAsync(request.Body, cancellationToken); }
        catch (InvalidDataException) { return ZohoWebhookValidationResult.Invalid("zoho.webhook.payload_invalid"); }

        if (!SignatureMatches(secret, signatures[0]!, body)) return ZohoWebhookValidationResult.Invalid("zoho.webhook.signature_invalid");
        return Parse(body);
    }

    public static string CreateSignatureForTesting(string secret, ReadOnlySpan<byte> body) => "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body));

    private static async Task<byte[]> ReadBoundedAsync(Stream stream, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var count = await stream.ReadAsync(chunk, cancellationToken);
            if (count == 0) return buffer.ToArray();
            if (buffer.Length + count > MaximumPayloadBytes) throw new InvalidDataException();
            await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
        }
    }

    private static bool SignatureMatches(string secret, string supplied, ReadOnlySpan<byte> body)
    {
        if (!supplied.StartsWith("sha256=", StringComparison.Ordinal) || supplied.Length != 71) return false;
        try
        {
            var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
            var actual = Convert.FromHexString(supplied[7..]);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException) { return false; }
    }

    private static ZohoWebhookValidationResult Parse(ReadOnlySpan<byte> body)
    {
        try
        {
            using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 16, CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !HasExactly(root, "eventType", "task") || !TryString(root, "eventType", 64, out var eventType) || eventType is not ("task.created" or "task.updated") || !root.TryGetProperty("task", out var task) || task.ValueKind != JsonValueKind.Object || !HasOnly(task, "id", "revision", "projectId", "channel", "title", "description", "attachments") || !TryIdentifier(task, "id", 128, out var taskId) || !TryIdentifier(task, "revision", 128, out var revision) || !TryIdentifier(task, "projectId", 128, out var projectId) || !TryChannel(task, out var channel) || !TryText(task, "title", 500, false, out var title) || !TryText(task, "description", 16 * 1024, true, out var description) || !TryAttachments(task, out var attachments)) return ZohoWebhookValidationResult.Invalid("zoho.webhook.payload_invalid");
            var idempotency = ZohoWebhookIdempotencyKey.Create(eventType, taskId, revision);
            return idempotency.IsSuccess
                ? ZohoWebhookValidationResult.Valid(new ZohoTaskWebhook(eventType, taskId, revision, projectId, channel, title, description, attachments), idempotency.Value!)
                : ZohoWebhookValidationResult.Invalid("zoho.webhook.payload_invalid");
        }
        catch (JsonException) { return ZohoWebhookValidationResult.Invalid("zoho.webhook.payload_invalid"); }
    }

private static bool HasExactly(JsonElement value, params string[] expected) => value.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal);
    private static bool HasOnly(JsonElement value, params string[] allowed) => value.EnumerateObject().All(property => allowed.Contains(property.Name, StringComparer.Ordinal));
    private static bool TryChannel(JsonElement parent, out string value) => TryString(parent, "channel", 32, out value) && value is "generic_task" or "feature_request" or "bug" or "change_request";
    private static bool TryAttachments(JsonElement ticket, out IReadOnlyList<ZohoAttachment> attachments)
    {
        attachments = [];
        if (!ticket.TryGetProperty("attachments", out var value)) return true;
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 5) return false;
        var parsed = new List<ZohoAttachment>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !HasExactly(item, "id", "fileName", "contentType", "sizeBytes") || !TryIdentifier(item, "id", 128, out var id) || !TryText(item, "fileName", 180, false, out var fileName) || !TryText(item, "contentType", 100, false, out var contentType) || !item.TryGetProperty("sizeBytes", out var size) || !size.TryGetInt64(out var sizeBytes) || sizeBytes < 1) return false;
            parsed.Add(new ZohoAttachment(id, fileName, contentType, sizeBytes));
        }
        attachments = parsed;
        return true;
    }
    private static bool TryIdentifier(JsonElement parent, string name, int maximumLength, out string value) => TryString(parent, name, maximumLength, out value) && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    private static bool TryText(JsonElement parent, string name, int maximumLength, bool allowNewlines, out string value) => TryString(parent, name, maximumLength, out value) && value.All(character => !char.IsControl(character) || (allowNewlines && character is '\r' or '\n' or '\t'));
    private static bool TryString(JsonElement parent, string name, int maximumLength, out string value)
    {
        value = string.Empty;
        return parent.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String && (value = property.GetString() ?? string.Empty).Length > 0 && value.Length <= maximumLength;
    }
}