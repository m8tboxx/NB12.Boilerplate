using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling;

namespace NB12.Boilerplate.BuildingBlocks.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiBuildingBlocks(this IServiceCollection services)
        {
            // Minimal API binding errors -> Exception, damit unser ExceptionHandler greift
            services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);

            services.AddProblemDetails();
            services.AddExceptionHandler<ApiExceptionHandler>();

            services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationMiddlewareResultHandler>();
            services.AddSingleton<IProblemDetailsMapper, ProblemDetailsMapper>();

            return services;
        }
    }
}
