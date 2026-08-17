using Quartz;

public class MyJob(ILogger<MyJob> logger) : IJob
{
    private readonly ILogger<MyJob> _logger = logger;

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation(
            "MyJob executed at {Time}",
            DateTimeOffset.Now);

        // Do your actual work here
        await Task.CompletedTask;
    }
}