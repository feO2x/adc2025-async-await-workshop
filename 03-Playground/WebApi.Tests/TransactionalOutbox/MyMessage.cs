using System;
using WebApi.TransactionalOutbox;

namespace WebApi.Tests.TransactionalOutbox;

[MessageType("MyMessage")]
public sealed record MyMessage(Guid CorrelationId, string Content) : IHasCorrelationId;