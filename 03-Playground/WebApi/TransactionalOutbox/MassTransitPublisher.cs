using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using WebApi.DatabaseAccess.Model;

namespace WebApi.TransactionalOutbox;

public sealed class MassTransitPublisher : IOutboxItemPublisher
{
    private readonly IBus _massTransitBus;
    private readonly MessageTypes _messageTypes;

    public MassTransitPublisher(IBus massTransitBus, MessageTypes messageTypes)
    {
        _massTransitBus = massTransitBus;
        _messageTypes = messageTypes;
    }

    public Task PublishOutboxItemAsync(OutboxItem outboxItem, CancellationToken cancellationToken = default)
    {
        if (!_messageTypes.TryGetDotnetType(outboxItem.MessageType, out var dotnetType))
        {
            throw new InvalidDataException("Unknown message type: " + outboxItem.MessageType);
        }

        var message = JsonSerializer.Deserialize(outboxItem.SerializedMessage, dotnetType);
        if (message is null)
        {
            throw new InvalidDataException("Failed to deserialize message of type " + dotnetType);
        }

        return _massTransitBus.Publish(
            message,
            context => context.CorrelationId = outboxItem.CorrelationId,
            cancellationToken
        );
    }
}