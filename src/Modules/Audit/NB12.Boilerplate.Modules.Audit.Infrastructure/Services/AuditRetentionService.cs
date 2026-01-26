using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Options;
using NB12.Boilerplate.Modules.Audit.Application.Responses;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;

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
            var o = _options.CurrentValue;
            _state.SetEnabled(o.Enabled);

            if (!o.Enabled)
            {
                return new AuditRetentionCleanupResultDto(
                    RanAtUtc: utcNow,
                    DeletedAuditLogs: 0,
                    DeletedErrorLogs: 0);
            }

            try
            {
                var auditCutoff = utcNow.AddDays(-o.RetainAuditLogsDays);
                var errorCutoff = utcNow.AddDays(-o.RetainErrorLogsDays);

                var deletedAudit = await _db.AuditLogs
                .Where(x => x.OccurredAtUtc < auditCutoff)
                .ExecuteDeleteAsync(ct);

                var deletedErrors = await _db.ErrorLogs
                    .Where(x => x.OccurredAtUtc < errorCutoff)
                    .ExecuteDeleteAsync(ct);

                _state.SetSuccess(utcNow, deletedAudit, deletedErrors);

                return new AuditRetentionCleanupResultDto(
                    RanAtUtc: utcNow,
                    DeletedAuditLogs: deletedAudit,
                    DeletedErrorLogs: deletedErrors);
            }
            catch(Exception ex)
            {
                _state.SetFailure(utcNow, ex.ToString());
                throw;
            }
        }
    }
}
