using MyRunshaw.Application.Buses.Services;
using Quartz;

namespace MyRunshaw.Workers.Jobs;

[DisallowConcurrentExecution]
public class BusRouteScraperJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BusRouteScraperJob> _logger;

    public BusRouteScraperJob(IServiceProvider serviceProvider, ILogger<BusRouteScraperJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting bus route scrape at {Time}", DateTimeOffset.Now);

        try
        {
            using var scope = _serviceProvider.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<BusRouteScraperService>();

            await service.RefreshAsync();

            _logger.LogInformation("Bus route scrape completed successfully.");
        }
        catch (Exception ex)
        {
            // don't stop the job scheduler if one scrape fails, just log it and try again next time
            _logger.LogError(ex, "Failed to scrape bus routes.");

            throw new JobExecutionException(ex);
        }
    }
}