using WebApi.DatabaseAccess.Model;

namespace WebApi.TransactionalOutbox;

public interface IMessageToOutboxItemConverter
{
    OutboxItem Convert(object message);
}