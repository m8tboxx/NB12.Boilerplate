using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Domain;
using NB12.Boilerplate.Modules.Auth.Domain.Events;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    internal sealed class ThrowingUserProfileCreatedHandler : IDomainEventHandler<UserProfileCreatedDomainEvent>
    {
        public Task Handle(UserProfileCreatedDomainEvent domainEvent, CancellationToken ct)
            => throw new InvalidOperationException("Test exception from domain event handler.");
    }
}
