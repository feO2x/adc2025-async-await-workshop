using Light.GuardClauses;

namespace AsyncStreamingGrpc.Tests.AsyncStreaming;

public sealed record AsyncStreamingTestOptions
{
    private readonly int _consumer2CancelAfter = 10;
    private readonly int _consumer3CancelAfter = 15;

    public required bool AreTestsEnabled { get; init; } = true;

    public required int Consumer2CancelAfter
    {
        get => _consumer2CancelAfter;
        init => _consumer2CancelAfter = value.MustBeGreaterThan(0);
    }

    public required int Consumer3CancelAfter
    {
        get => _consumer3CancelAfter;
        init => _consumer3CancelAfter = value.MustBeGreaterThan(0);
    }
}