using System;
using System.Text.Json;
using Light.GuardClauses;
using WebApi.DatabaseAccess.Model;

namespace WebApi.TransactionalOutbox;

public sealed class MessageToOutboxItemConverter : IMessageToOutboxItemConverter
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly MessageTypes _messageTypes;
    private readonly TimeProvider _timeProvider;

    public MessageToOutboxItemConverter(
        MessageTypes messageTypes,
        TimeProvider timeProvider,
        JsonSerializerOptions jsonOptions
    )
    {
        _messageTypes = messageTypes;
        _timeProvider = timeProvider;
        _jsonOptions = jsonOptions;
    }

    public OutboxItem Convert(object message)
    {
        message.MustNotBeNull();
        var dotnetType = message.GetType();
        if (!_messageTypes.TryGetMessageType(dotnetType, out var messageType))
        {
            throw new InvalidOperationException(
                $"No message type found for type \"{dotnetType}\". Please ensure to register all message types with the {nameof(MessageTypes)} class."
            );
        }

        var correlationId = message is IHasCorrelationId messageWithCorrelationId ?
            messageWithCorrelationId.CorrelationId :
            Guid.NewGuid();

        return new OutboxItem
        {
            MessageType = messageType,
            CorrelationId = correlationId,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            SerializedMessage = JsonSerializer.Serialize(message, dotnetType, _jsonOptions)
        };
    }
}