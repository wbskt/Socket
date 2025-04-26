using Wbskt.Server.Services;

namespace Wbskt.Server
{
    public class TaskProcessorHostedService(ILogger<TaskProcessor> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Start your TaskProcessor with logging and cancellation
            await Task.Run(() => TaskProcessor.GetInstance().Run(logger, stoppingToken), stoppingToken);
        }
    }
}
