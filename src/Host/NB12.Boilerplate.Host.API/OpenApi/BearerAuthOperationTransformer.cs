using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NB12.Boilerplate.Host.API.OpenApi
{
    internal sealed class BearerAuthOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(
            OpenApiOperation operation,
            OpenApiOperationTransformerContext context,
            CancellationToken cancellationToken)
        {
            var metadata = context.Description.ActionDescriptor?.EndpointMetadata;

            // Wenn AllowAnonymous gesetzt ist: keine SecurityRequirement -> Scalar zeigt "no auth"
            if (metadata?.OfType<IAllowAnonymous>().Any() == true)
                return Task.CompletedTask;

            // Wenn überhaupt kein Authorize drauf ist: NICHT automatisch Auth erzwingen
            // (Wenn du global eine FallbackPolicy hast, dann kannst du hier anders entscheiden.)
            //if (metadata?.OfType<IAuthorizeData>().Any() != true)
            //    return Task.CompletedTask;

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });

            return Task.CompletedTask;
        }
    }
}
