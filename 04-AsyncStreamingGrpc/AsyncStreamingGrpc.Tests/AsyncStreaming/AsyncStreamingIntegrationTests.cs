using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncStreamingGrpc.AsyncStreaming;
using FluentAssertions;
using Grpc.Core;
using Light.Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace AsyncStreamingGrpc.Tests.AsyncStreaming;

public sealed class AsyncStreamingIntegrationTests : IAsyncLifetime
{
    private static readonly AsyncStreamingTestOptions Options =
        TestSettings.Configuration.GetSection("asyncStreaming").Get<AsyncStreamingTestOptions>()!;

    private readonly WebApiFactory _factory;
    private readonly ILogger _logger;

    public AsyncStreamingIntegrationTests(ITestOutputHelper output)
    {
        _logger = output.CreateTestLogger();
        _factory = new WebApiFactory(_logger);
    }

    // When you implement an interface and one of the methods should do noting, return Task.CompletedTask and
    // do not mark the method async. Avoid using Task.Run or similar things as they are needless overhead.
    // You enqueue a delegate on the thread pool that executes nothing.
    public Task InitializeAsync() => Task.CompletedTask;

    // If the last statement in a method produces the task for the caller, you can avoid the async keyword by
    // simply return this task from another method. ValueTask can be converted to Task with the AsTask method.
    public Task DisposeAsync() => _factory.DisposeAsync().AsTask();

    [SkippableFact]
    public async Task StreamNumbersToSeveralClients()
    {
        SkipIfNecessary();

        var numberGeneratorOptions = _factory.Services.GetRequiredService<NumberGeneratorOptions>();
        using var grpcChannel = _factory.CreateGrpcChannel();
        var consumer1 = new Consumer1(grpcChannel.CreateGrpcService<INumberStreamingService>(), _logger);
        var consumer2 = new Consumer2(grpcChannel.CreateGrpcService<INumberStreamingService>(), _logger);
        var consumer3 = new Consumer3(grpcChannel.CreateGrpcService<INumberStreamingService>(), _logger);

        var consumer1Task = consumer1.ReceiveNumbers();
        var consumer2Task = consumer2.ReceiveNumbers();
        var consumer3Task = consumer3.ReceiveNumbers();
        await grpcChannel.CreateGrpcService<INumberStreamingService>().StartStreaming();

        // If you wait on a task and want to timeout, an easy way is to use WaitAsync. It will throw when the
        // specified timeout interval is reached. In more complex scenarios, Polly.net is probably the better choice.
        await Task
           .WhenAll(consumer1Task, consumer2Task, consumer3Task)
           .WaitAsync(TimeSpan.FromSeconds(numberGeneratorOptions.AmountOfNumbers + 10));

        consumer1.ReceivedNumbers.Should().Equal(Enumerable.Range(1, numberGeneratorOptions.AmountOfNumbers));
        consumer2.ReceivedNumbers.Should().Equal(Enumerable.Range(1, Options.Consumer2CancelAfter));
        consumer3.ReceivedNumbers.Should().Equal(Enumerable.Range(1, numberGeneratorOptions.AmountOfNumbers));
    }

    [SkippableFact]
    public async Task StreamNumbersToSeveralClientsWithCancellation()
    {
        SkipIfNecessary();

        var numberGeneratorOptions = _factory.Services.GetRequiredService<NumberGeneratorOptions>();
        using var grpcChannel = _factory.CreateGrpcChannel();
        var consumer1 = new Consumer1(grpcChannel.CreateGrpcService<INumberStreamingService>(), _logger);
        var consumer2 = new Consumer2(grpcChannel.CreateGrpcService<INumberStreamingService>(), _logger);
        var consumer3 = new Consumer3(grpcChannel.CreateGrpcService<INumberStreamingService>(), _logger)
            { IsCancellationEnabled = true };

        var consumer1Task = consumer1.ReceiveNumbers();
        var consumer2Task = consumer2.ReceiveNumbers();
        var consumer3Task = consumer3.ReceiveNumbers();
        await grpcChannel.CreateGrpcService<INumberStreamingService>().StartStreaming();
        await Task
           .WhenAll(consumer1Task, consumer2Task, consumer3Task)
           .WaitAsync(TimeSpan.FromSeconds(numberGeneratorOptions.AmountOfNumbers + 10));

        consumer1.ReceivedNumbers.Should().Equal(Enumerable.Range(1, Options.Consumer3CancelAfter));
        consumer2.ReceivedNumbers.Should().Equal(Enumerable.Range(1, Options.Consumer2CancelAfter));
        consumer3.ReceivedNumbers.Should().Equal(Enumerable.Range(1, Options.Consumer3CancelAfter));
    }

    private static void SkipIfNecessary() =>
        Skip.IfNot(TestSettings.Configuration.GetValue<bool>("asyncStreaming:areTestsEnabled"));

    // Consumer 1 wants to receive all numbers, it performs no cancellation.
    private sealed class Consumer1
    {
        private readonly ILogger _logger;
        private readonly INumberStreamingService _proxy;

        public Consumer1(INumberStreamingService proxy, ILogger logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public List<int> ReceivedNumbers { get; } = new ();

        public async Task ReceiveNumbers()
        {
            try
            {
                var stream = _proxy.StreamNumbers(StreamNumbersRequestDto.Default);
                await foreach (var dto in stream)
                {
                    _logger.Information("Consumer 1: Received number {Number}", dto.Number);
                    ReceivedNumbers.Add(dto.Number);
                }
            }
            catch (RpcException e) when (e.InnerException is OperationCanceledException)
            {
                _logger.Information("Consumer 1: Stream was cancelled");
            }
            catch (Exception e)
            {
                _logger.Error(e, "Consumer 1: An error occurred during async streaming");
            }
        }
    }

    // Consumer 2 is a standard caller that cancels the stream after a certain number of elements.
    private sealed class Consumer2
    {
        private readonly ILogger _logger;
        private readonly INumberStreamingService _proxy;

        public Consumer2(INumberStreamingService proxy, ILogger logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public List<int> ReceivedNumbers { get; } = new ();

        public async Task ReceiveNumbers()
        {
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                var stream = _proxy.StreamNumbers(StreamNumbersRequestDto.Default);
                await foreach (var dto in stream.WithCancellation(cancellationTokenSource.Token))
                {
                    _logger.Information("Consumer 2: Received number {Number}", dto.Number);
                    ReceivedNumbers.Add(dto.Number);
                    if (dto.Number == Options.Consumer2CancelAfter)
                    {
                        await cancellationTokenSource.CancelAsync();
                    }
                }
            }
            catch (RpcException e) when (e.InnerException is OperationCanceledException)
            {
                _logger.Information("Consumer 2: Stream was cancelled");
            }
            catch (Exception e)
            {
                _logger.Error(e, "Consumer 2: An error occurred during async streaming");
            }
        }
    }

    // Consumer 3 is an admin caller that cancels all streams after a certain number of elements depending on the
    // IsCancellationEnabled property.
    private sealed class Consumer3
    {
        private readonly ILogger _logger;
        private readonly INumberStreamingService _proxy;

        public Consumer3(INumberStreamingService proxy, ILogger logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public bool IsCancellationEnabled { get; init; }
        public List<int> ReceivedNumbers { get; } = new ();

        public async Task ReceiveNumbers()
        {
            try
            {
                // Be sure to dispose CancellationTokenSource after usage, otherwise you end up with memory leaks.
                using var cancellationTokenSource = new CancellationTokenSource();
                var stream = _proxy.StreamNumbers(
                    new StreamNumbersRequestDto { CancelAllStreams = true },
                    cancellationTokenSource.Token
                );
                await foreach (var dto in stream)
                {
                    _logger.Information("Consumer 3: Received number {Number}", dto.Number);
                    ReceivedNumbers.Add(dto.Number);
                    if (dto.Number == Options.Consumer3CancelAfter && IsCancellationEnabled)
                    {
                        await cancellationTokenSource.CancelAsync();
                    }
                }
            }
            catch (RpcException e) when (e.InnerException is OperationCanceledException)
            {
                _logger.Information("Consumer 3: Stream was cancelled");
            }
            catch (Exception e)
            {
                _logger.Error(e, "Consumer 3: An error occurred during async streaming");
            }
        }
    }
}