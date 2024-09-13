using System.Threading;
using System.Threading.Tasks;
using WebApi.DatabaseAccess.Model;

namespace WebApi.TransactionalOutbox;

public interface IOutboxItemPublisher
{
    Task PublishOutboxItemAsync(OutboxItem outboxItem, CancellationToken cancellationToken = default);
}