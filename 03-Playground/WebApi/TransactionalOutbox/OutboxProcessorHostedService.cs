using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace WebApi.TransactionalOutbox;

public sealed class OutboxProcessorHostedService : IHostedService
{
    private readonly CancellationTokenSource _cancellationTokenSource = new ();
    private readonly ILogger _logger;
    private readonly OutboxProcessorOptions _options;
    private readonly IOutboxProcessor _outboxProcessor;

    public OutboxProcessorHostedService(
        IOutboxProcessor outboxProcessor,
        OutboxProcessorOptions options,
        ILogger logger
    )
    {
        _outboxProcessor = outboxProcessor;
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.IsEnabled)
        {
            _logger.Information("Starting periodic outbox processing");
            ProcessOutboxItemsPeriodically();
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled)
        {
            return;
        }

        try
        {
            _logger.Information("Stopping outbox processor");
            await _cancellationTokenSource.CancelAsync();
        }
        catch (AggregateException exception)
        {
            _logger.Information(exception, "Cancelling the outbox processor timer");
        }

        await _outboxProcessor.CancelOutboxProcessingAsync();
        _cancellationTokenSource.Dispose();
    }

    private async void ProcessOutboxItemsPeriodically()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try
        {
            if (await timer.WaitForNextTickAsync(_cancellationTokenSource.Token))
            {
                try
                {
                    await _outboxProcessor.TryTriggerOutboxAsync(-1);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "An error occurred while processing outbox items");
                }
            }
        }
        catch (OperationCanceledException exception)
        {
            _logger.Information(exception, "Outbox processor was cancelled");
        }
    }
}