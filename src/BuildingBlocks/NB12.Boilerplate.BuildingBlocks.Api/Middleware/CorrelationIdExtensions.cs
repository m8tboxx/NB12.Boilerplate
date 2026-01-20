using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace NB12.Boilerplate.BuildingBlocks.Api.Middleware
{
    public static class CorrelationIdExtensions
    {
        public static IServiceCollection AddCorrelationId(this IServiceCollection services)
            => services.AddTransient<CorrelationIdMiddleware>();

        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
            => app.UseMiddleware<CorrelationIdMiddleware>();

        public static string? GetCorrelationId(this HttpContext http)
            => http.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var v) ? v?.ToString() : null;
    }
}
