using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace NB12.Boilerplate.BuildingBlocks.Api.Middleware.ETag
{
    public static class ETagExtensions
    {
        public static IServiceCollection AddETag(this IServiceCollection services, Action<ETagOptions>? configure = null)
        {
            if (configure is not null)
                services.Configure(configure);
            else
                services.Configure<ETagOptions>(_ => { });

            services.AddTransient<ETagMiddleware>();
            return services;
        }

        public static IApplicationBuilder UseETag(this IApplicationBuilder app)
            => app.UseMiddleware<ETagMiddleware>();
    }
}
