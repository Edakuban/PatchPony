namespace PatchPony.Core.Common;

public sealed record DomainError(string Code, string Message)
{
    public static readonly DomainError None = new(string.Empty, string.Empty);

    public static DomainError Validation(string message) => new("validation.invalid", message);

    public static DomainError Conflict(string code, string message) => new(code, message);
}
