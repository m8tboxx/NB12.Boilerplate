using NB12.Boilerplate.Modules.Audit.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Services
{
    internal sealed class AuditRetentionStatusState : IAuditRetentionStatusProvider
    {
        private AuditRetentionStatusDto _status = new(
            Enabled: false,
            LastRunAtUtc: null,
            LastDeletedAuditLogs: null,
            LastDeletedErrorLogs: null,
            LastError: null);

        public AuditRetentionStatusDto GetStatus() => _status;

        public void SetEnabled(bool enabled)
            => _status = _status with { Enabled = enabled };

        public void SetSuccess(DateTime ranAtUtc, int deletedAuditLogs, int deletedErrorLogs)
            => _status = new AuditRetentionStatusDto(
                Enabled: _status.Enabled,
                LastRunAtUtc: ranAtUtc,
                LastDeletedAuditLogs: deletedAuditLogs,
                LastDeletedErrorLogs: deletedErrorLogs,
                LastError: null);

        public void SetFailure(DateTime failedAtUtc, string error)
            => _status = new AuditRetentionStatusDto(
                Enabled: _status.Enabled,
                LastRunAtUtc: failedAtUtc,
                LastDeletedAuditLogs: _status.LastDeletedAuditLogs,
                LastDeletedErrorLogs: _status.LastDeletedErrorLogs,
                LastError: error);
    }
}
