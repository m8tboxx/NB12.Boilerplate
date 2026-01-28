using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Domain;
using NB12.Boilerplate.Modules.Auth.Domain.Events;

namespace NB12.Boilerplate.Host.Api.IntegrationTests
{
    public sealed class ThrowingDomainEventsWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.AddScoped<IDomainEventHandler<UserProfileCreatedDomainEvent>, ThrowingUserProfileCreatedHandler>();
        }
    }
}
