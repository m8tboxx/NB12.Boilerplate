using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.BuildingBlocks.Domain.Enums;
using NB12.Boilerplate.BuildingBlocks.Domain.Exceptions;
using System.Diagnostics;
using System.Text.Json;

namespace NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling
{
    public sealed class ProblemDetailsMapper : IProblemDetailsMapper
    {
        private readonly IHostEnvironment _env;

        public ProblemDetailsMapper(IHostEnvironment env)
            => _env = env;

        public ProblemDetails FromErrors(HttpContext http, IReadOnlyList<Error> errors)
        {
            var primary = errors.Count > 0
            ? errors[0]
            : Error.Failure("error.unknown", "Unknown error.");

            var status = MapStatus(primary.Type);
            var title = MapTitle(primary.Type);

            var pd = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = primary.Message,
                Type = $"urn:nb12:error:{primary.Code}",
                Instance = http.Request.Path.Value
            };

            Enrich(http, pd);
            pd.Extensions["errors"] = errors.Select(ApiErrorDto.From).ToArray();
            return pd;
        }

        public ProblemDetails FromException(HttpContext http, Exception exception)
        {
            // Minimal API Binding / Bad request (ThrowOnBadRequest = true)
            if (exception is BadHttpRequestException bad)
            {
                var e = Error.Validation("http.bad_request", bad.Message);
                return FromErrors(http, new[] { e });
            }

            // Invalid JSON payload
            if (exception is JsonException)
            {
                var e = Error.Validation("http.invalid_json", "Invalid JSON payload.");
                return FromErrors(http, new[] { e });
            }

            // Domain rule violations -> 400
            if (exception is BusinessRuleValidationException br)
            {
                var e = Error.Validation("domain.rule_violated", br.Message);
                return FromErrors(http, new[] { e });
            }

            // FluentValidation -> 400 (Details strukturiert)
            if (exception is ValidationException fv)
            {
                var errors = fv.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.ErrorMessage).ToArray());

                var pd = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation",
                    Type = "urn:nb12:error:validation",
                    Instance = http.Request.Path.Value
                };

                Enrich(http, pd);
                return pd;
            }

            // Auth
            if (exception is UnauthorizedAccessException)
                return FromErrors(http, new[] { Error.Unauthorized("auth.unauthorized", "Unauthorized.") });

            // Fallback -> 500 (Detail nur in Dev)
            var detail = _env.IsDevelopment()
                ? exception.ToString()
                : "An unexpected error occurred.";

            var pd500 = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = detail,
                Type = "urn:nb12:error:internal",
                Instance = http.Request.Path.Value
            };

            Enrich(http, pd500);
            return pd500;
        }

        private static int MapStatus(ErrorType type) => type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

        private static string MapTitle(ErrorType type) => type switch
        {
            ErrorType.Validation => "Validation",
            ErrorType.NotFound => "Not Found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.Failure => "Internal Server Error",
            _ => "Internal Server Error"
        };

        private static void Enrich(HttpContext http, ProblemDetails pd)
        {
            var traceId = Activity.Current?.Id ?? http.TraceIdentifier;
            pd.Extensions["traceId"] = traceId;
            pd.Extensions["timestampUtc"] = DateTimeOffset.UtcNow;
        }
    }
}
