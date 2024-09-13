using System.Threading.Tasks;

namespace WebApi.TransactionalOutbox;

public interface IOutboxTrigger
{
    ValueTask<bool> TryTriggerOutboxAsync(int timeoutInMilliseconds = 50);
}