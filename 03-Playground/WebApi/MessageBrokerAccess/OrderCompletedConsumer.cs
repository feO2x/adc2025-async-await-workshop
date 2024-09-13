using System.Threading.Tasks;
using MassTransit;
using Serilog;
using WebApi.Orders;

namespace WebApi.MessageBrokerAccess;

public sealed class OrderCompletedConsumer : IConsumer<OrderCompleted>
{
    private readonly ILogger _logger;

    public OrderCompletedConsumer(ILogger logger) => _logger = logger;

    public Task Consume(ConsumeContext<OrderCompleted> context)
    {
        var order = context.Message.Order;
        _logger.Information("OrderCompleted received for {OrderId}", order.Id);
        return Task.CompletedTask;
    }
}