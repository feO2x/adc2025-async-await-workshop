using System;
using Light.SharedCore.Entities;

namespace WebApi.DatabaseAccess.Model;

public sealed class OutboxItem : Int64Entity
{
    public required string MessageType { get; init; }
    public required Guid CorrelationId { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required string SerializedMessage { get; init; }
}