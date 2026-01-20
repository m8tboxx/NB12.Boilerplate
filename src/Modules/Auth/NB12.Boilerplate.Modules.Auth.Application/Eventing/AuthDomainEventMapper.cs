using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Domain.Events;
using NB12.Boilerplate.Modules.Auth.Domain.Events;
using NB12.Boilerplate.Modules.Auth.Contracts.IntegrationEvents;

namespace NB12.Boilerplate.Modules.Auth.Application.Eventing
{
    public sealed class AuthDomainEventMapper : IDomainEventToIntegrationEventMapper
    {
        public IEnumerable<IIntegrationEvent> Map(IDomainEvent domainEvent)
        {
            if (domainEvent is UserProfileCreatedDomainEvent e)
            {
                yield return new UserCreatedIntegrationEvent(e.IdentityUserId, e.Email);
            }
        }
    }
}
