using System;
using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DatabaseAccessAbstractions;
using WebApi.DatabaseAccess.Model;

namespace WebApi.Orders;

public interface ICompleteOrderDbSession : ISession
{
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);
    void AddMessageAsOutboxItem(object message);
}