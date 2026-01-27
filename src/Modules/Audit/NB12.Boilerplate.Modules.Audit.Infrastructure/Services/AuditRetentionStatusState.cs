using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Services
{
    public sealed class AuditRetentionStatusState
    {
        private readonly object _lock = new();

        private bool _enabled;
        private DateTime? _lastRunAtUtc;
        private long _lastDeletedAuditLogs;
        private long _lastDeletedErrorLogs;
        private string? _lastError;

        public void SetEnabled(bool enabled)
        {
            lock (_lock)
            {
                _enabled = enabled;
            }
        }

        public void RecordSuccess(DateTime runAtUtc, long deletedAuditLogs, long deletedErrorLogs)
        {
            lock (_lock)
            {
                _lastRunAtUtc = runAtUtc;
                _lastDeletedAuditLogs = deletedAuditLogs;
                _lastDeletedErrorLogs = deletedErrorLogs;
                _lastError = null;
            }
        }

        public void RecordError(DateTime runAtUtc, string error)
        {
            lock (_lock)
            {
                _lastRunAtUtc = runAtUtc;
                _lastError = error;
            }
        }

        public AuditRetentionStatusDto Snapshot()
        {
            lock (_lock)
            {
                return new AuditRetentionStatusDto(
                    Enabled: _enabled,
                    LastRunAtUtc: _lastRunAtUtc,
                    LastDeletedAuditLogs: _lastDeletedAuditLogs,
                    LastDeletedErrorLogs: _lastDeletedErrorLogs,
                    LastError: _lastError);
            }
        }
    }
}
