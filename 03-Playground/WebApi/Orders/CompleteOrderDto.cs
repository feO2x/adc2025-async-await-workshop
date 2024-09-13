using System;

namespace WebApi.Orders;

public sealed record CompleteOrderDto(Guid OrderId);