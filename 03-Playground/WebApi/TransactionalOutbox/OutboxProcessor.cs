using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Serilog;
using WebApi.DatabaseAccess.Model;

namespace WebApi.TransactionalOutbox;

public sealed class OutboxProcessor : IOutboxProcessor, IDisposable
{
    private readonly ILogger _logger;
    private readonly OutboxProcessorOptions _options;
    private readonly Func<OutboxProcessor, CancellationToken, ValueTask> _processAsyncDelegate;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly SemaphoreSlim _semaphore = new (1, 1);
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly List<OutboxItem> _successfullyProcessedOutboxItems;
    private CurrentOperation? _currentOperation;

    public OutboxProcessor(
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OutboxProcessorOptions> options,
        ILogger logger
    )
    {
        _resiliencePipeline = resiliencePipelineProvider.GetPipeline(OutboxConstants.ResiliencePipelineKey);
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
        _processAsyncDelegate = (op, ct) => op.ProcessAsync(ct);
        _successfullyProcessedOutboxItems = new List<OutboxItem>();
    }

    public void Dispose() => _semaphore.Dispose();

    public async ValueTask<bool> TryTriggerOutboxAsync(int timeoutInMilliseconds = 50) =>
        throw new NotImplementedException();

    public Task WaitForOutboxCompletionAsync() => throw new NotImplementedException();

    public async Task CancelOutboxProcessingAsync() => throw new NotImplementedException();

    private async void HandleCurrentTask(Task task)
    {
        // This method is async void because we do not want callers of TryTriggerOutboxAsync to wait until the whole
        // outbox processing is done. The calling method cannot track the task associated with this async method and
        // thus returns early. To accomodate this, the await task.ConfigureAwait(false) call is wrapped in a try-catch
        // block so that we do not lose any exceptions that might occur during the processing.
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "An error occurred during outbox processing");
        }

        // We will not use any cancellation token or timeout here because we absolutely must enter a critical section.
        // Otherwise, other services won't be able to trigger the outbox processing again in the future.
        await _semaphore.WaitAsync();
        Volatile.Write(ref _currentOperation, null); // Could be just a simple assignment
        _semaphore.Release();
    }

    private async ValueTask ProcessAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var session = scope.ServiceProvider.GetRequiredService<IOutboxProcessorDbSession>();
            var outboxItems = await session.LoadNextOutboxItemsAsync(_options.BatchSize, cancellationToken);
            if (outboxItems.Count == 0)
            {
                return;
            }

            await SendOutboxItemsAsync(scope, session, outboxItems, cancellationToken);
        }
    }

    private async Task SendOutboxItemsAsync(
        AsyncServiceScope scope,
        IOutboxProcessorDbSession session,
        List<OutboxItem> outboxItems,
        CancellationToken cancellationToken
    )
    {
        _successfullyProcessedOutboxItems.Clear();

        var outboxItemPublisher = scope.ServiceProvider.GetRequiredService<IOutboxItemPublisher>();

        try
        {
            foreach (var outboxItem in outboxItems)
            {
                await outboxItemPublisher.PublishOutboxItemAsync(outboxItem, cancellationToken);
                _successfullyProcessedOutboxItems.Add(outboxItem);
                _logger.Information("Successfully published outbox item {@OutboxItem}", outboxItem);
            }
        }
        finally
        {
            // No matter if all or only a part of the outbox items have been published,
            // we try to remove the successfully published ones
            // from the database to avoid sending them again in the future.
            if (_successfullyProcessedOutboxItems.Count > 0)
            {
                await session.RemoveOutboxItemsAsync(_successfullyProcessedOutboxItems, cancellationToken);
                await session.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private sealed record CurrentOperation(Task Task, CancellationTokenSource CancellationTokenSource) : IDisposable
    {
        public void Dispose() => CancellationTokenSource.Dispose();
    }
}