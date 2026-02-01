using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace NB12.Boilerplate.Host.Shared.Ops
{
    internal sealed class OpsAllowAnonymousFilter(IHostEnvironment env) : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            // Hide ops endpoints outside dev unless authenticated (you can harden further later)
            if (!env.IsDevelopment())
                return Results.NotFound();

            return await next(context);
        }
    }
}
