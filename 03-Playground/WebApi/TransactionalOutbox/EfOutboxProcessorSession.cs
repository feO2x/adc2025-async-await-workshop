using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Light.DatabaseAccess.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebApi.DatabaseAccess;
using WebApi.DatabaseAccess.Model;

namespace WebApi.TransactionalOutbox;

public sealed class EfOutboxProcessorSession : EfSession<WebApiDbContext>.WithTransaction,
                                               IOutboxProcessorDbSession
{
    public EfOutboxProcessorSession(WebApiDbContext dbContext) : base(
        dbContext,
        IsolationLevel.ReadCommitted,
        QueryTrackingBehavior.NoTracking
    ) { }

    public async Task<List<OutboxItem>> LoadNextOutboxItemsAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        await using var command = await CreateCommandAsync<NpgsqlCommand>(
            """
            SELECT "Id", "MessageType", "CorrelationId", "CreatedAtUtc", "SerializedMessage"
            FROM "OutboxItems"
            ORDER BY "Id"
            LIMIT $1
            FOR UPDATE SKIP LOCKED;
            """,
            cancellationToken
        );
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = batchSize });
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken);
        var outboxItems = new List<OutboxItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var outboxItem = new OutboxItem
            {
                Id = reader.GetInt64(0),
                MessageType = reader.GetString(1),
                CorrelationId = reader.GetGuid(2),
                CreatedAtUtc = reader.GetDateTime(3),
                SerializedMessage = reader.GetString(4)
            };
            outboxItems.Add(outboxItem);
        }

        return outboxItems;
    }

    public Task RemoveOutboxItemsAsync(
        List<OutboxItem> successfullyProcessedOutboxItems,
        CancellationToken cancellationToken = default
    )
    {
        var outboxItemIds = successfullyProcessedOutboxItems.Select(oi => oi.Id).ToList();
        return DbContext
           .OutboxItems
           .Where(oi => outboxItemIds.Contains(oi.Id))
           .ExecuteDeleteAsync(cancellationToken);
    }
}