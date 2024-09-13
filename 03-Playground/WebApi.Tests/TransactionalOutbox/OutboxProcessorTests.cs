using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using Serilog;
using WebApi.DatabaseAccess.Model;
using WebApi.TransactionalOutbox;
using Xunit;
using Xunit.Abstractions;

namespace WebApi.Tests.TransactionalOutbox;

public sealed class OutboxProcessorTests : IAsyncLifetime
{
    public static readonly TheoryData<List<OutboxFailure>> OutboxFailures =
        new ()
        {
            new List<OutboxFailure> { OutboxFailure.ErrorAtLoadNextOutboxItems },
            new List<OutboxFailure> { OutboxFailure.ErrorAtPublishOutboxItem },
            new List<OutboxFailure>
            {
                OutboxFailure.ErrorAtLoadNextOutboxItems,
                OutboxFailure.None,
                OutboxFailure.ErrorAtLoadNextOutboxItems
            },
            new List<OutboxFailure>
            {
                OutboxFailure.None,
                OutboxFailure.ErrorAtLoadNextOutboxItems,
                OutboxFailure.ErrorAtLoadNextOutboxItems,
                OutboxFailure.None,
                OutboxFailure.ErrorAtPublishOutboxItem,
                OutboxFailure.None,
                OutboxFailure.ErrorAtRemoveOutboxItems,
                OutboxFailure.ErrorAtSaveChanges,
                OutboxFailure.None,
                OutboxFailure.ErrorAtPublishOutboxItem | OutboxFailure.ErrorAtRemoveOutboxItems,
                OutboxFailure.ErrorAtPublishOutboxItem | OutboxFailure.ErrorAtSaveChanges
            }
        };

    private readonly OutboxFailureContext _failureContext;
    private readonly OutboxItemPublisherMock _outboxItemPublisher;
    private readonly OutboxProcessor _outboxProcessor;
    private readonly ServiceProvider _serviceProvider;
    private readonly OutboxProcessorSessionMockFactory _sessionFactory;

    public OutboxProcessorTests(ITestOutputHelper testOutputHelper)
    {
        var logger = new LoggerConfiguration()
           .MinimumLevel.Debug()
           .WriteTo.TestOutput(testOutputHelper)
           .CreateLogger();
        var configuration = new ConfigurationBuilder()
           .AddInMemoryCollection(new Dictionary<string, string?> { ["TransactionalOutbox:BatchSize"] = "3" })
           .Build();

        _failureContext = new OutboxFailureContext();
        _sessionFactory = new OutboxProcessorSessionMockFactory(_failureContext);
        _outboxItemPublisher = new OutboxItemPublisherMock(_failureContext);

        _serviceProvider = new ServiceCollection()
           .AddSingleton<ILogger>(logger)
           .AddSingleton<IConfiguration>(configuration)
           .AddTransactionalOutbox(registerDefaultResiliencePipeline: false)
           .AddResiliencePipeline(
                OutboxConstants.ResiliencePipelineKey,
                pipelineBuilder => pipelineBuilder.AddRetry(
                    new RetryStrategyOptions
                    {
                        MaxRetryAttempts = 100,
                        Delay = TimeSpan.Zero,
                        BackoffType = DelayBackoffType.Constant
                    }
                )
            )
           .AddScoped<IOutboxProcessorDbSession>(_ => _sessionFactory.Create())
           .AddSingleton<IOutboxItemPublisher>(_outboxItemPublisher)
           .BuildServiceProvider();
        _outboxProcessor = _serviceProvider.GetRequiredService<OutboxProcessor>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _serviceProvider.DisposeAsync().AsTask();

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    public async Task OutboxProcessorShouldWorkCorrectlyWhenNoErrorsOccur(int amountOfItems)
    {
        var outboxItems = CreateOutboxItems(amountOfItems);

        await RunOutboxProcessor();

        _sessionFactory
           .OutboxItemsShouldBeEmpty()
           .AllSessionsShouldBeCommittedAndDisposed();
        _outboxItemPublisher.ShouldHaveReceivedExactlyOnce(outboxItems);
    }

    [Theory]
    [MemberData(nameof(OutboxFailures))]
    public async Task OutboxProcessorShouldRetryWhenErrorsOccur(List<OutboxFailure> failures)
    {
        var outboxItems = CreateOutboxItems(10);
        _failureContext.Failures.AddRange(failures);

        await RunOutboxProcessor();

        _sessionFactory
           .OutboxItemsShouldBeEmpty()
           .AllSuccessfulSessionsShouldBeCommitted()
           .AllSessionsShouldBeDisposed();
        _outboxItemPublisher.ShouldHaveReceivedAtLeastOnce(outboxItems);
    }

    [Theory]
    [InlineData(50, 3)]
    [InlineData(100, 10)]
    public async Task ConcurrentOutboxTriggersShouldOnlyResultInOneProcessing(
        int numberOfItems,
        int degreeOfParallelism
    )
    {
        var outboxItems = CreateOutboxItems(numberOfItems);

        var triggerTasks = new Task[degreeOfParallelism];
        for (var i = 0; i < triggerTasks.Length; i++)
        {
            triggerTasks[i] = Task.Run(() => _outboxProcessor.TryTriggerOutboxAsync());
        }

        await Task.WhenAll(triggerTasks);
        await _outboxProcessor.WaitForOutboxCompletionAsync();

        _sessionFactory
           .OutboxItemsShouldBeEmpty()
           .AllSessionsShouldBeCommittedAndDisposed();
        _outboxItemPublisher.ShouldHaveReceivedExactlyOnce(outboxItems);
    }

    private List<OutboxItem> CreateOutboxItems(int amount)
    {
        var outboxItems = new List<OutboxItem>(amount);
        for (var i = 0; i < amount; i++)
        {
            var item = new OutboxItem
            {
                Id = i + 1,
                MessageType = nameof(MyMessage),
                SerializedMessage = JsonSerializer.Serialize(new MyMessage(Guid.NewGuid(), $"Message Content {i + 1}")),
                CreatedAtUtc = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid()
            };
            outboxItems.Add(item);
        }

        _sessionFactory.OutboxItems.AddRange(outboxItems);

        return outboxItems;
    }

    private async Task RunOutboxProcessor()
    {
        await _outboxProcessor.TryTriggerOutboxAsync();
        await _outboxProcessor.WaitForOutboxCompletionAsync();
    }
}