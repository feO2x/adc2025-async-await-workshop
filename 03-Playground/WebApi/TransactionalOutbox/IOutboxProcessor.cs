using System.Threading.Tasks;

namespace WebApi.TransactionalOutbox;

public interface IOutboxProcessor : IOutboxTrigger, IAwaitOutboxCompletion
{
    Task CancelOutboxProcessingAsync();
}