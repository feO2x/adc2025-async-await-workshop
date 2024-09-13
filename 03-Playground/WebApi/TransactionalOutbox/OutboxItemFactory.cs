using System;
using System.Text.Json;
using Light.GuardClauses;
using WebApi.DatabaseAccess.Model;

namespace WebApi.TransactionalOutbox;

public sealed class OutboxItemFactory : IOutboxItemFactory
{
    private readonly MessageTypes _messageTypes;
    private readonly TimeProvider _timeProvider;

    public OutboxItemFactory(MessageTypes messageTypes, TimeProvider timeProvider)
    {
        _messageTypes = messageTypes;
        _timeProvider = timeProvider;
    }

    public OutboxItem CreateFromMessage(object message)
    {
        message.MustNotBeNull();

        var messageDotnetType = message.GetType();
        if (!_messageTypes.TryGetMessageType(messageDotnetType, out var messageType))
        {
            throw new ArgumentException(
                "The message is not decorated with the messageTypes attribute",
                nameof(message)
            );
        }

        var correlationId = message is IHasCorrelationId messageWithCorrelationId ?
            messageWithCorrelationId.CorrelationId :
            Guid.NewGuid();

        var outboxItem = new OutboxItem
        {
            CorrelationId = correlationId,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            MessageType = messageType,
            SerializedMessage = JsonSerializer.Serialize(message, messageDotnetType)
        };
        return outboxItem;
    }
}