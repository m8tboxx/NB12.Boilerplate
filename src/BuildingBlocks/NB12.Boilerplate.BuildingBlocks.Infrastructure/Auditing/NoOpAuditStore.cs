using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Auditing
{
    public sealed class NoOpAuditStore : IAuditStore
    {
        public Task WriteEntityChangesAsync(IReadOnlyCollection<EntityChangeAudit> entries, AuditContext context, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task WriteErrorAsync(ErrorAudit error, AuditContext context, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
