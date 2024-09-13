using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WebApi.DatabaseAccess.Model;

// ReSharper disable MemberCanBePrivate.Global

namespace WebApi.Orders;

public static class CompleteOrderEndpoint
{
    public static void MapCompleteOrder(this WebApplication app) => app.MapPut("/api/orders", CompleteOrder);

    public static async Task<IResult> CompleteOrder(
        CompleteOrderDto dto,
        ICompleteOrderDbSession dbSession,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken = default
    )
    {
        var order = await dbSession.GetOrderAsync(dto.OrderId, cancellationToken);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        order.State = OrderState.Completed;
        dbSession.AddMessageAsOutboxItem(new OrderCompleted(order));
        await dbSession.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}