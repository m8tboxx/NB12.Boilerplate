using NB12.Boilerplate.BuildingBlocks.Application.Auditing;

namespace NB12.Boilerplate.BuildingBlocks.Application.Interfaces
{
    public interface IAuditStore
    {
        Task WriteEntityChangesAsync(
            IReadOnlyCollection<EntityChangeAudit> entries,
            AuditContext context,
            CancellationToken ct = default);

        Task WriteErrorAsync(
            ErrorAudit error,
            AuditContext context,
            CancellationToken ct = default);
    }
}
