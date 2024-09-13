using System.Threading;
using System.Threading.Tasks;
using Light.DatabaseAccess.EntityFrameworkCore;
using Light.SharedCore.DatabaseAccessAbstractions;
using Microsoft.EntityFrameworkCore;
using WebApi.TransactionalOutbox;

namespace WebApi.DatabaseAccess;

public abstract class EfOutboxSession : EfSession<WebApiDbContext>, ISession
{
    private readonly IOutboxItemFactory _outboxItemFactory;
    private readonly IOutboxTrigger _outboxProcessor;

    protected EfOutboxSession(
        IOutboxItemFactory outboxItemFactory,
        IOutboxTrigger outboxProcessor,
        WebApiDbContext dbContext,
        QueryTrackingBehavior queryTrackingBehavior = QueryTrackingBehavior.TrackAll
    ) : base(dbContext, queryTrackingBehavior)
    {
        _outboxItemFactory = outboxItemFactory;
        _outboxProcessor = outboxProcessor;
    }

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveChangesAsync(cancellationToken);
        await _outboxProcessor.TryTriggerOutboxAsync();
    }

    public void AddMessageAsOutboxItem(object message)
    {
        var outboxItem = _outboxItemFactory.CreateFromMessage(message);
        DbContext.OutboxItems.Add(outboxItem);
    }
}