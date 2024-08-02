using GtfsFeedValidator.Services;

namespace GtfsFeedValidator
{
    public sealed class GtfsValidatorWorker(IGtfsFeedValidatorWorkerService service, ILogger<GtfsValidatorWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug("starting validation");

                await service.ProcessQueueAsync(stoppingToken);

                logger.LogDebug("ending validation");

                await Task.Delay(Constants.WorkerMsPollingInterval, stoppingToken);
            }
        }
    }
}
