using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Auditing
{
    public sealed class NoOpErrorAuditWriter : IErrorAuditWriter
    {
        public Task WriteErrorAsync(ErrorAudit error, AuditContext context, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
