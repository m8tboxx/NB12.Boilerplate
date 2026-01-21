using NB12.Boilerplate.BuildingBlocks.Application.Auditing;

namespace NB12.Boilerplate.BuildingBlocks.Application.Interfaces
{
    public interface IErrorAuditWriter
    {
        Task WriteErrorAsync(
            ErrorAudit error,
            AuditContext context,
            CancellationToken ct = default);
    }
}
