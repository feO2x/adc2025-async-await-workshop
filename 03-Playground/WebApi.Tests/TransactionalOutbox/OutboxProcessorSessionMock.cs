using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Light.DataAccessMocks;
using WebApi.DatabaseAccess.Model;
using WebApi.TransactionalOutbox;

namespace WebApi.Tests.TransactionalOutbox;

public sealed class OutboxProcessorSessionMock : SessionMock, IOutboxProcessorDbSession
{
    private readonly List<OutboxItem> _outboxItems;

    public OutboxProcessorSessionMock(List<OutboxItem> outboxItems, OutboxFailure failure)
    {
        _outboxItems = outboxItems;
        Failure = failure;
    }

    public OutboxFailure Failure { get; }

    public async Task<List<OutboxItem>> LoadNextOutboxItemsAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Delay(1, cancellationToken);
        if (Failure.HasFlagValue(OutboxFailure.ErrorAtLoadNextOutboxItems))
        {
            throw new IOException("LoadNextOutboxItems failed");
        }

        return _outboxItems.Take(batchSize).ToList();
    }

    public async Task RemoveOutboxItemsAsync(
        List<OutboxItem> successfullyProcessedOutboxItems,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Delay(1, cancellationToken);
        if (Failure.HasFlagValue(OutboxFailure.ErrorAtRemoveOutboxItems))
        {
            throw new IOException("RemoveOutboxItems failed");
        }

        foreach (var outboxItem in successfullyProcessedOutboxItems)
        {
            _outboxItems.Remove(outboxItem);
        }
    }

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        if (Failure.HasFlagValue(OutboxFailure.ErrorAtSaveChanges))
        {
            throw new IOException("SaveChanges failed");
        }

        await base.SaveChangesAsync(cancellationToken);
    }
}