using PatchPony.Core.Common;

namespace PatchPony.Core.Application;

public sealed class RuntimeStatusService(ICorrelationContext correlations)
{
    public RuntimeStatus Get() => new("PatchPony", "read-only", correlations.Current.Value);

    public Result<CorrelationValidation> ValidateCorrelation(string? value)
    {
        var correlation = CorrelationId.Create(value);
        return correlation.IsSuccess
            ? Result<CorrelationValidation>.Success(new CorrelationValidation(correlation.Value!.Value))
            : Result<CorrelationValidation>.Failure(correlation.Error);
    }
}

public sealed record RuntimeStatus(string Service, string Mode, string CorrelationId);
public sealed record CorrelationValidation(string CorrelationId);
