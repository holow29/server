﻿namespace Bit.EventsProcessor;

public class AzureQueueHostedService : BackgroundService, IDisposable
{
    private readonly IProcessor _processor;
    private readonly ILogger<AzureQueueHostedService> _logger;
    private readonly IDisposable _loggerScope;

    private readonly IConfiguration _configuration;

    public AzureQueueHostedService(
        IProcessor processor, ILogger<AzureQueueHostedService> logger, IConfiguration configuration)
    {
        _processor = processor;
        _logger = logger;
        _configuration = configuration;
        _loggerScope = _logger.BeginScope("BackgroundService: AzureQueueHostedService");
    }

    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var storageConnectionString = _configuration["azureStorageConnectionString"];
        if (string.IsNullOrWhiteSpace(storageConnectionString))
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var _ = _logger.BeginScope("Executing {RunId}", Guid.NewGuid());
                var didProcess = await _processor.ProcessAsync(cancellationToken);
                if (!didProcess)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while processing events queue.");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        _logger.LogWarning("Done processing.");
    }

    public override void Dispose()
    {
        _loggerScope.Dispose();
        base.Dispose();
    }
}
