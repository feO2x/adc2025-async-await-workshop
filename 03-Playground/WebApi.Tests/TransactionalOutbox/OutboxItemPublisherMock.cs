using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WebApi.DatabaseAccess.Model;
using WebApi.TransactionalOutbox;

namespace WebApi.Tests.TransactionalOutbox;

public sealed class OutboxItemPublisherMock : IOutboxItemPublisher
{
    private readonly OutboxFailureContext _failureContext;
    private readonly List<OutboxItem> _receivedOutboxItems = new ();

    public OutboxItemPublisherMock(OutboxFailureContext failureContext) => _failureContext = failureContext;

    public Task PublishOutboxItemAsync(OutboxItem outboxItem, CancellationToken cancellationToken = default)
    {
        var currentFailure = _failureContext.CurrentFailure;
        if (currentFailure.HasFlagValue(OutboxFailure.ErrorAtPublishOutboxItem))
        {
            throw new IOException("PublishOutboxItem failed");
        }

        _receivedOutboxItems.Add(outboxItem);
        return Task.CompletedTask;
    }

    public OutboxItemPublisherMock ShouldHaveReceivedAtLeastOnce(List<OutboxItem> outboxItems)
    {
        outboxItems.Should().Equal(_receivedOutboxItems.Distinct());
        return this;
    }

    public OutboxItemPublisherMock ShouldHaveReceivedExactlyOnce(List<OutboxItem> outboxItems)
    {
        outboxItems.Should().Equal(_receivedOutboxItems);
        return this;
    }
}