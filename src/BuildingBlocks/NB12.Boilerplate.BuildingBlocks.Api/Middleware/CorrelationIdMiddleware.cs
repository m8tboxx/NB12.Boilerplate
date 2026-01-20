using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace NB12.Boilerplate.BuildingBlocks.Api.Middleware
{
    public sealed class CorrelationIdMiddleware : IMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        public const string ItemKey = "CorrelationId";

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var cid = GetOrCreate(context);

            context.Items[ItemKey] = cid;

            // Response immer zurückgeben
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = cid;
                return Task.CompletedTask;
            });

            // Optional: Activity Baggage/Tag (für Tracing)
            Activity.Current?.SetBaggage("correlation_id", cid);
            Activity.Current?.SetTag("correlation_id", cid);

            await next(context);
        }

        private static string GetOrCreate(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var existing) &&
                !string.IsNullOrWhiteSpace(existing))
            {
                return existing.ToString();
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
