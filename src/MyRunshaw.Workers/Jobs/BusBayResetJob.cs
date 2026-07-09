// At midnight, this job resets all the bays to zero. I'd hope the buses would be gone by then!
using MyRunshaw.Application.Buses;
using Quartz;

[DisallowConcurrentExecution]
public class BusBayResetJob : IJob
{
    private readonly IBusRepository _repository;
    private readonly ILogger<BusBayResetJob> _logger;

    public BusBayResetJob(
        IBusRepository repository,
        ILogger<BusBayResetJob> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Resetting all bus bays.");

        await _repository.ResetAllBusesToZeroAsync();

        _logger.LogInformation("Bus bay reset complete.");
    }
}