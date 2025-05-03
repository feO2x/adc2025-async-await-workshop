using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf.Grpc;

namespace AsyncStreamingGrpc.AsyncStreaming;

public sealed class NumberStreamingService : INumberStreamingService
{
    private readonly NumberGenerator _numberGenerator;

    public NumberStreamingService(NumberGenerator numberGenerator) => _numberGenerator = numberGenerator;

    public IAsyncEnumerable<StreamNumbersResponseDto> StreamNumbers(
        StreamNumbersRequestDto dto,
        CallContext context = default
    ) =>
        dto.CancelAllStreams ?
            StreamForAdminCallerAsync(context.CancellationToken) :
            StreamForStandardCallerAsync(context.CancellationToken);

    public Task<StreamingResponseDto> StartStreaming(CallContext callContext = default)
    {
        var result = _numberGenerator.TryStartGeneratingNumbers();
        return Task.FromResult(new StreamingResponseDto { IsSuccess = result });
    }
    
    // Not necessary because we register explicitly on the cancellation token
#pragma warning disable CS8425 // Async-iterator member has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
    private async IAsyncEnumerable<StreamNumbersResponseDto> StreamForAdminCallerAsync(
        CancellationToken cancellationToken
    )
    {
        await using var registration = cancellationToken.Register(
            state => ((NumberGenerator) state!).TryStopGeneratingNumbers(),
            _numberGenerator
        );

        // ReSharper disable once UseCancellationTokenForIAsyncEnumerable -- already handled by the registration
        await foreach (var number in _numberGenerator.GetAsyncEnumerable())
        {
            yield return new StreamNumbersResponseDto { Number = number };
        }
    }
#pragma warning restore CS8425
    
    private async IAsyncEnumerable<StreamNumbersResponseDto> StreamForStandardCallerAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await foreach (var number in _numberGenerator.GetAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return StreamNumbersResponseDto.FromNumber(number);
        }
    }
}