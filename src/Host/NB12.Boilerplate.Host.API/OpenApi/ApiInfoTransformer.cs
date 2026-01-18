using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NB12.Boilerplate.Host.API.OpenApi
{
    internal sealed class ApiInfoTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            document.Info = new OpenApiInfo
            {
                Title = "NB12 Boilerplate API",
                Version = "v1",
                Description = "Modular Monolith API (Auth module, etc.)"
            };

            return Task.CompletedTask;
        }
    }
}
