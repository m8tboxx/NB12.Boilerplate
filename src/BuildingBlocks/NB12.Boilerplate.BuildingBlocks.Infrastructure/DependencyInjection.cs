using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Application.Security;
using NB12.Boilerplate.BuildingBlocks.Domain.Serialization;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Auditing;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Auth;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Eventing;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration.Admin;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Inbox;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Outbox;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers cross-cutting infrastructure services that must be available to all modules.
        /// </summary>
        public static IServiceCollection AddInfrastructureBuildingBlocks(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            // Current user accessor (reads from HttpContext)
            services.AddSingleton<ICurrentUser, CurrentUser>();

            // Permission-based authorization (policy is built on-demand)
            services.AddSingleton<IPermissionCatalog, PermissionCatalog>();
            services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            // Audit core
            services.AddSingleton<IErrorAuditWriter, NoOpErrorAuditWriter>(); // wird vom Audit-Modul überschrieben
            services.AddSingleton<IAuditContextAccessor, DefaultAuditContextAccessor>();
            services.AddSingleton<AuditSaveChangesInterceptor>();

            services.AddSingleton(_ => AppJsonSerializerOptions.Create());
            services.AddSingleton<CompositeDomainEventToIntegrationEventMapper>();
            services.AddSingleton<DomainEventsOutboxInterceptor>();

            // Admin-Resolvers MUST be scoped (otherwise: scoped from root provider)
            services.AddScoped<IOutboxAdminStoreResolver, OutboxAdminStoreResolver>();
            services.AddScoped<IInboxAdminStoreResolver, InboxAdminStoreResolver>();

            return services;
        }
    }
}
