using NB12.Boilerplate.BuildingBlocks.Application.Auditing;

namespace NB12.Boilerplate.BuildingBlocks.Application.Interfaces
{
    public interface IAuditContextAccessor
    {
        AuditContext GetCurrent();
    }
}
