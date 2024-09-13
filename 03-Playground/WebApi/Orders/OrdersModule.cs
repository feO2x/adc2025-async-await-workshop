using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Orders;

public static class OrdersModule
{
    public static IServiceCollection AddOrders(this IServiceCollection services) =>
        services.AddScoped<ICompleteOrderDbSession, EfCompleteOrderSession>();
}