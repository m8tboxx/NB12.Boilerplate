using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Persistence;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Options;
using NB12.Boilerplate.Modules.Audit.Application.Responses;
using NB12.Boilerplate.Modules.Audit.Domain.Entities;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;
using Npgsql;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Services
{
    internal sealed class AuditRetentionService : IAuditRetentionService
    {
        private readonly AuditDbContext _db;
        private readonly IOptionsMonitor<AuditRetentionOptions> _options;
        private readonly AuditRetentionStatusState _state;

        public AuditRetentionService(
            AuditDbContext db, 
            IOptionsMonitor<AuditRetentionOptions> options,
            AuditRetentionStatusState state)
        {
            _db = db;
            _options = options;
            _state = state;
        }

        public AuditRetentionConfigDto GetConfig()
        {
            var o = _options.CurrentValue;
            _state.SetEnabled(o.Enabled);

            return new AuditRetentionConfigDto(
                o.Enabled,
                o.RunEveryMinutes,
                o.RetainAuditLogsDays,
                o.RetainErrorLogsDays);
        }

        public async Task<AuditRetentionCleanupResultDto> RunCleanupAsync(DateTime utcNow, CancellationToken ct)
        {
            var opts = _options.CurrentValue;
            _state.SetEnabled(opts.Enabled);

            if (!opts.Enabled)
            {
                return new AuditRetentionCleanupResultDto(
                    RanAtUtc: utcNow,
                    DeletedAuditLogs: 0,
                    DeletedErrorLogs: 0);
            }

            var deletedAudit = 0;
            var deletedErrors = 0;

            try
            {
                if(opts.RetainAuditLogsDays > 0)
                {
                    var auditCutoff = utcNow.AddDays(-opts.RetainAuditLogsDays);

                    deletedAudit = await DeleteBatchedAsync<AuditLog>(
                        timestampPropertyName: nameof(AuditLog.OccurredAtUtc),
                        cutoffUtc: auditCutoff,
                        batchSize: opts.BatchSize,
                        maxRows: opts.MaxRowsPerRun,
                        ct: ct);

                    //deletedAudit = await _db.AuditLogs
                    //    .Where(x => x.OccurredAtUtc < auditCutoff)
                    //    .ExecuteDeleteAsync(ct);
                }

                if (opts.RetainErrorLogsDays > 0)
                {
                    var errorCutoff = utcNow.AddDays(-opts.RetainErrorLogsDays);

                    deletedErrors = await DeleteBatchedAsync<ErrorLog>(
                        timestampPropertyName: nameof(ErrorLog.OccurredAtUtc),
                        cutoffUtc: errorCutoff,
                        batchSize: opts.BatchSize,
                        maxRows: opts.MaxRowsPerRun,
                        ct: ct);

                    //deletedErrors = await _db.ErrorLogs
                    //    .Where(x => x.OccurredAtUtc < errorCutoff)
                    //    .ExecuteDeleteAsync(ct);
                }
                
                _state.RecordSuccess(utcNow, deletedAudit, deletedErrors);

                return new AuditRetentionCleanupResultDto(
                    RanAtUtc: utcNow,
                    DeletedAuditLogs: deletedAudit,
                    DeletedErrorLogs: deletedErrors);
            }
            catch(Exception ex)
            {
                _state.RecordError(utcNow, ex.ToString());
                throw;
            }
        }

        public Task RunOnceAsync(CancellationToken ct)
            => RunCleanupAsync(DateTime.UtcNow, ct);


        private async Task<int> DeleteBatchedAsync<TEntity>(
            string timestampPropertyName,
            DateTime cutoffUtc,
            int batchSize,
            int maxRows,
            CancellationToken ct)
            where TEntity : class
        {
            // Pruning / Guards
            if (batchSize < 1) batchSize = 1;
            if (batchSize > 50_000) batchSize = 50_000;

            if (maxRows < 1) maxRows = 1;
            if (maxRows > 1_000_000) maxRows = 1_000_000;

            var (table, storeId, et) = EfPostgresSql.Table<TEntity>(_db);
            var tsCol = EfPostgresSql.Column(et, storeId, timestampPropertyName);

            var deletedTotal = 0;

            while (deletedTotal < maxRows)
            {
                ct.ThrowIfCancellationRequested();

                var remaining = maxRows - deletedTotal;
                var limit = remaining < batchSize ? remaining : batchSize;

                var sql = $@"
                    DELETE FROM {table}
                    WHERE ctid IN (
                        SELECT ctid
                        FROM {table}
                        WHERE {tsCol} < @cutoff
                        ORDER BY {tsCol}
                        LIMIT @limit
                    );";

                var affected = await _db.Database.ExecuteSqlRawAsync(
                    sql,
                    new NpgsqlParameter("cutoff", cutoffUtc),
                    new NpgsqlParameter("limit", limit),
                    ct);

                deletedTotal += affected;

                // Wenn weniger als Limit gelöscht wurde, sind wir fertig.
                if (affected < limit)
                    break;
            }

            return deletedTotal;
        }
    }
}
