using System;
using System.Collections.Generic;
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
        throw new NotImplementedException();

    public Task<StreamingResponseDto> StartStreaming(CallContext callContext = default)
    {
        var result = _numberGenerator.TryStartGeneratingNumbers();
        return Task.FromResult(new StreamingResponseDto { IsSuccess = result });
    }
}