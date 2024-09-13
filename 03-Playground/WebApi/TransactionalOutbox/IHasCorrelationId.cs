using System;

namespace WebApi.TransactionalOutbox;

public interface IHasCorrelationId
{
    Guid CorrelationId { get; }
}