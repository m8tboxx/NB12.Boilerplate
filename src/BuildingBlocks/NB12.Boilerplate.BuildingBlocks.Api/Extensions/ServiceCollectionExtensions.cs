using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Api.Middleware;
using NB12.Boilerplate.BuildingBlocks.Api.Middleware.ETag;
using NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling;

namespace NB12.Boilerplate.BuildingBlocks.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiBuildingBlocks(this IServiceCollection services)
        {
            // Minimal API binding errors -> Exception, damit unser ExceptionHandler greift
            services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);

            services.AddCorrelationId();

            services.AddProblemDetails();
            services.AddExceptionHandler<ApiExceptionHandler>();

            services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationMiddlewareResultHandler>();
            services.AddSingleton<IProblemDetailsMapper, ProblemDetailsMapper>();

            services.AddETag(options =>
            {
                options.MaxBodySizeBytes = 1_000_000;
                options.SetCacheControlIfMissing = true;
                options.CacheControlValue = "private, max-age=0, must-revalidate";
            });

            return services;
        }
    }
}
