using NB12.Boilerplate.Modules.Audit.Application.Responses;

namespace NB12.Boilerplate.Modules.Audit.Application.Interfaces
{
    public interface IAuditRetentionStatusProvider
    {
        AuditRetentionStatusDto GetStatus();
    }
}
