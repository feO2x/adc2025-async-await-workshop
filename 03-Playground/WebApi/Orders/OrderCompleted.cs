using System;
using WebApi.DatabaseAccess.Model;
using WebApi.TransactionalOutbox;

namespace WebApi.Orders;

[MessageType("OrderCompleted")]
public sealed record OrderCompleted(Order Order) : IHasCorrelationId
{
    Guid IHasCorrelationId.CorrelationId => Order.Id;
}