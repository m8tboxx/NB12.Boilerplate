using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.BuildingBlocks.Api.Extensions
{
    public static class StatusCodePagesProblemDetailsExtensions
    {
        public static IApplicationBuilder UseStatusCodeProblemDetails(this IApplicationBuilder app)
        {
            return app.UseStatusCodePages(async ctx =>
            {
                var http = ctx.HttpContext;

                if (http.Response.HasStarted)
                    return;

                // Nicht überschreiben, wenn schon Content gesetzt ist
                if (!string.IsNullOrWhiteSpace(http.Response.ContentType))
                    return;

                // Nicht überschreiben, wenn Body/Content-Length schon gesetzt ist
                if (http.Response.ContentLength is > 0)
                    return;

                var mapper = http.RequestServices.GetRequiredService<IProblemDetailsMapper>();
                var pds = http.RequestServices.GetRequiredService<IProblemDetailsService>();

                var pd = mapper.FromErrors(http, MapStatus(http.Response.StatusCode));

                var statusCode = http.Response.StatusCode;

                pd.Status = statusCode;
                pd.Title = MapTitleFromStatusCode(statusCode, pd.Title);
                pd.Type = $"urn:nb12:http:{statusCode}";

                http.Response.ContentType = "application/problem+json";

                await pds.TryWriteAsync(new()
                {
                    HttpContext = http,
                    ProblemDetails = pd
                });
            });
        }

        private static IReadOnlyList<Error> MapStatus(int statusCode)
            => statusCode switch
            {
                StatusCodes.Status404NotFound => new[]
                {
                Error.NotFound("http.not_found", "Endpoint not found.")
                },
                StatusCodes.Status405MethodNotAllowed => new[]
                {
                Error.Failure("http.method_not_allowed", "HTTP method not allowed.")
                },
                StatusCodes.Status415UnsupportedMediaType => new[]
                {
                Error.Validation("http.unsupported_media_type", "Unsupported media type.")
                },
                StatusCodes.Status401Unauthorized => new[]
                {
                Error.Unauthorized("auth.not_authenticated", "Not authenticated.")
                },
                StatusCodes.Status403Forbidden => new[]
                {
                Error.Forbidden("auth.forbidden", "Forbidden.")
                },
                _ => new[]
                {
                Error.Failure($"http.status_{statusCode}", $"HTTP request failed with status code {statusCode}.")
                }
            };

        private static string MapTitleFromStatusCode(int statusCode, string? fallback)
            => statusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status404NotFound => "Not Found",
                StatusCodes.Status405MethodNotAllowed => "Method Not Allowed",
                StatusCodes.Status415UnsupportedMediaType => "Unsupported Media Type",
                StatusCodes.Status500InternalServerError => "Internal Server Error",
                _ => fallback ?? "Request failed"
            };
    }
}
