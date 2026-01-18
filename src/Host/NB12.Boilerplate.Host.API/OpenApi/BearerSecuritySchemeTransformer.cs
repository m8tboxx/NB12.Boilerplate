using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NB12.Boilerplate.Host.API.OpenApi
{
    internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();
            var hasBearer = schemes.Any(s => string.Equals(s.Name, "Bearer", StringComparison.OrdinalIgnoreCase));

            if (!hasBearer)
                return;

            document.Components ??= new OpenApiComponents();

            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT"
                }
            };

            //foreach (var pathItem in document.Paths.Values)
            //{
            //    if(pathItem.Operations is null)
            //        continue;

            //    foreach (var operation in pathItem.Operations.Values)
            //    {
            //        operation.Security ??= [];
            //        operation.Security.Add(new OpenApiSecurityRequirement
            //        {
            //            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            //        });
            //    }
            //}
        }
    }
}
