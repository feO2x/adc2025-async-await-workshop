using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Light.DatabaseAccess.EntityFrameworkCore;
using Light.SharedCore.DatabaseAccessAbstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace WebApi.DatabaseAccess;

public abstract class NpgsqlBatchSession : EfSession<WebApiDbContext>.WithTransaction,
                                           ISession,
                                           IDisposable
{
    private NpgsqlBatch? _npgsqlBatch;

    protected NpgsqlBatchSession(
        WebApiDbContext dbContext,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        QueryTrackingBehavior queryTrackingBehavior = QueryTrackingBehavior.TrackAll
    ) : base(dbContext, isolationLevel, queryTrackingBehavior) { }

    public new void Dispose()
    {
        _npgsqlBatch?.Dispose();
        base.Dispose();
    }

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_npgsqlBatch is null)
        {
            throw new InvalidOperationException(
                "You must call any of the batch methods before calling SaveChangesAsync"
            );
        }

        await _npgsqlBatch.ExecuteNonQueryAsync(cancellationToken);
        await base.SaveChangesAsync(cancellationToken);
    }

    public new async ValueTask DisposeAsync()
    {
        if (_npgsqlBatch is not null)
        {
            await _npgsqlBatch.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    protected ValueTask<NpgsqlBatch> GetRequiredBatchAsync(CancellationToken cancellationToken) =>
        _npgsqlBatch is not null ? new ValueTask<NpgsqlBatch>(_npgsqlBatch) : InitializeBatchAsync(cancellationToken);

    private async ValueTask<NpgsqlBatch> InitializeBatchAsync(CancellationToken cancellationToken)
    {
        var dbContext = await GetDbContextAsync(cancellationToken);
        _npgsqlBatch = ((NpgsqlConnection) dbContext.Database.GetDbConnection()).CreateBatch();
        if (dbContext.Database.CurrentTransaction is IInfrastructure<DbTransaction> transactionProvider)
        {
            _npgsqlBatch.Transaction = (NpgsqlTransaction) transactionProvider.Instance;
        }

        return _npgsqlBatch;
    }
}