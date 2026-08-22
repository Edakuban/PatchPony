namespace PatchPony.Core.Common;

public readonly record struct CorrelationId(string Value)
{
    public static CorrelationId New() => new(Guid.NewGuid().ToString("N"));

    public static Result<CorrelationId> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
        {
            return Result<CorrelationId>.Failure(DomainError.Validation("A correlation identifier of at most 128 letters, digits, hyphens or underscores is required."));
        }

        return Result<CorrelationId>.Success(new CorrelationId(value.Trim()));
    }
}

public interface ICorrelationContext
{
    CorrelationId Current { get; }

    IDisposable BeginScope(CorrelationId correlationId);
}

public sealed class CorrelationContext : ICorrelationContext
{
    private readonly AsyncLocal<ScopeState?> current = new();

    public CorrelationId Current => current.Value?.CorrelationId
        ?? throw new InvalidOperationException("No correlation scope is active.");

    public IDisposable BeginScope(CorrelationId correlationId)
    {
        var previous = current.Value;
        current.Value = new ScopeState(correlationId, previous);
        return new Scope(this, previous);
    }

    private sealed record ScopeState(CorrelationId CorrelationId, ScopeState? Previous);

    private sealed class Scope(CorrelationContext context, ScopeState? previous) : IDisposable
    {
        private CorrelationContext? context = context;

        public void Dispose()
        {
            if (context is null)
            {
                return;
            }

            context.current.Value = previous;
            context = null;
        }
    }
}
