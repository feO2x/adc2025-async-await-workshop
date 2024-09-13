using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DatabaseAccessAbstractions;
using WebApi.DatabaseAccess.Model;

namespace WebApi.TransactionalOutbox;

public interface IOutboxProcessorDbSession : ISession
{
    Task<List<OutboxItem>> LoadNextOutboxItemsAsync(int batchSize, CancellationToken cancellationToken = default);

    Task RemoveOutboxItemsAsync(
        List<OutboxItem> successfullyProcessedOutboxItems,
        CancellationToken cancellationToken = default
    );
}