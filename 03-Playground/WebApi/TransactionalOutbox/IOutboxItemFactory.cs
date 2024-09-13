using WebApi.DatabaseAccess.Model;

namespace WebApi.TransactionalOutbox;

public interface IOutboxItemFactory
{
    OutboxItem CreateFromMessage(object message);
}