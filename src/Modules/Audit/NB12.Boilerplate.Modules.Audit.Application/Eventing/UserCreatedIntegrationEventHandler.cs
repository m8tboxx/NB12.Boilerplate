using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Contracts.IntegrationEvents;

namespace NB12.Boilerplate.Modules.Audit.Application.Eventing
{
    public sealed class UserCreatedIntegrationEventHandler(IAuditStore auditStore)
    : IIntegrationEventHandler<UserCreatedIntegrationEvent>
    {
        public Task Handle(UserCreatedIntegrationEvent e, CancellationToken ct)
        {
            var ctx = new AuditContext(
                OccurredAtUtc: e.OccurredAtUtc,
                UserId: e.IdentityUserId,
                Email: e.Email,
                TraceId: null,
                CorrelationId: null);

            var change = new EntityChangeAudit(
                EntityType: "Auth.User",
                EntityId: e.IdentityUserId,
                Operation: AuditOperation.Insert,
                Changes:
                [
                    new PropertyChange("IdentityUserId", null, e.IdentityUserId),
                    new PropertyChange("Email", null, e.Email)
                ]);

            return auditStore.WriteEntityChangesAsync([change], ctx, ct);
        }
    }
}
