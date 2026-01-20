using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;

namespace NB12.Boilerplate.Modules.Auth.Contracts.IntegrationEvents
{
    public sealed class UserCreatedIntegrationEvent(string identityUserId, string email) : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;

        public string IdentityUserId { get; } = identityUserId;
        public string Email { get; } = email;
    }
}
