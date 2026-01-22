using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing.Integration;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.BuildingBlocks.Application.Security;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Auditing;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Auth;
using NB12.Boilerplate.BuildingBlocks.Infrastructure.Eventing;
using System.Text.Json;

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
            services.AddScoped<ICurrentUser, CurrentUser>();

            // Permission-based authorization (policy is built on-demand)
            services.AddSingleton<IPermissionCatalog, PermissionCatalog>();
            services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            // Audit core
            services.AddSingleton<IErrorAuditWriter, NoOpErrorAuditWriter>(); // wird vom Audit-Modul überschrieben
            services.AddScoped<IAuditContextAccessor, DefaultAuditContextAccessor>();
            services.AddScoped<AuditSaveChangesInterceptor>();

            services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            services.AddScoped<CompositeDomainEventToIntegrationEventMapper>();
            services.AddScoped<DomainEventsOutboxInterceptor>();
            //services.AddEventBus(assemblies);

            return services;
        }
    }
}
