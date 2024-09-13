using System.Collections.Generic;
using System.Threading.Tasks;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace AsyncStreamingGrpc.AsyncStreaming;

[ProtoContract]
public sealed record StreamNumbersRequestDto
{
    public static StreamNumbersRequestDto Default { get; } = new ();

    [ProtoMember(1)]
    public bool CancelAllStreams { get; init; }
}

[ProtoContract]
public sealed record StreamNumbersResponseDto
{
    [ProtoMember(1)]
    public required int Number { get; init; }

    public static StreamNumbersResponseDto FromNumber(int number) => new () { Number = number };
}

[ProtoContract]
public sealed record StreamingResponseDto
{
    [ProtoMember(1)]
    public required bool IsSuccess { get; init; }
}

[Service]
public interface INumberStreamingService
{
    IAsyncEnumerable<StreamNumbersResponseDto> StreamNumbers(
        StreamNumbersRequestDto dto,
        CallContext context = default
    );

    Task<StreamingResponseDto> StartStreaming(CallContext callContext = default);
}