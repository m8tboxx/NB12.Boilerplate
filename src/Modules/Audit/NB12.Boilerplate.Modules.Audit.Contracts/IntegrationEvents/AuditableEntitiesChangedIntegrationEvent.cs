using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.Modules.Audit.Contracts.Auditing;

namespace NB12.Boilerplate.Modules.Audit.Contracts.IntegrationEvents
{
    public sealed record AuditableEntitiesChangedIntegrationEvent(
        Guid Id,
        DateTime OccurredAtUtc,
        string Module,
        string? UserId,
        string? Email,
        string? TraceId,
        string? CorrelationId,
        IReadOnlyCollection<AuditableEntityChange> Entries
    ) : IIntegrationEvent;
}
