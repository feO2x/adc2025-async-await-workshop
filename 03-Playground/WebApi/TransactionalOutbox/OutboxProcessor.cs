using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
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

    public async ValueTask<bool> TryTriggerOutboxAsync(int timeoutInMilliseconds = 50)
    {
        timeoutInMilliseconds.MustBeGreaterThanOrEqualTo(-1);

        // This method can be called from multiple threads concurrently. To make it thread-safe, we
        // use a semaphore with a double-check lock.
        if (Volatile.Read(ref _currentOperation) is not null)
        {
            return false;
        }

        // To not keep callers waiting indefinitely, we use a timeout to access the semaphore.
        // The default value is a maximum wait time of 300 milliseconds.
        if (!await _semaphore.WaitAsync(timeoutInMilliseconds).ConfigureAwait(false))
        {
            return false;
        }

        // If we end up here, we are in the critical section. We use a try-finally block to ensure that
        // the semaphore is released even if an exception occurs.
        try
        {
            // Here is the second part of the double-check lock.
            if (Volatile.Read(ref _currentOperation) is not null)
            {
                return false;
            }

            // If we end up here, we need to start processing of the outbox messages.
            var cancellationTokenSource = new CancellationTokenSource();
            var currentTask = _resiliencePipeline
               .ExecuteAsync(_processAsyncDelegate, this, cancellationTokenSource.Token)
               .AsTask();
            Volatile.Write(ref _currentOperation, new CurrentOperation(currentTask, cancellationTokenSource));
            HandleCurrentTask(currentTask);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task WaitForOutboxCompletionAsync()
    {
        // We do not need to enter the semaphore here because we simply copy the current task to a local variable.
        // This is an atomic operations on x86, x64, and ARM processors.
        var currentOperation = Volatile.Read(ref _currentOperation);
        return currentOperation?.Task ?? Task.CompletedTask;
    }

    public async Task CancelOutboxProcessingAsync()
    {
        var currentOperation = Volatile.Read(ref _currentOperation);
        if (currentOperation is null)
        {
            return;
        }

        try
        {
            await currentOperation.CancellationTokenSource.CancelAsync();
        }
        catch (AggregateException exception)
        {
            _logger.Information(exception, "Outbox processing has been cancelled");
        }
    }

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