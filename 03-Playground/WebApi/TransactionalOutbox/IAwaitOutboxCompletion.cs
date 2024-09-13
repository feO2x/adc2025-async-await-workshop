using System.Threading.Tasks;

namespace WebApi.TransactionalOutbox;

public interface IAwaitOutboxCompletion
{
    Task WaitForOutboxCompletionAsync();
}