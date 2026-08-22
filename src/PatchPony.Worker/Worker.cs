using PatchPony.Core.Common;

namespace PatchPony.Worker;

public class Worker(ILogger<Worker> logger, ICorrelationContext correlations) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var correlationId = CorrelationId.New();
            using var correlationScope = correlations.BeginScope(correlationId);
            using var logScope = logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId.Value });
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
