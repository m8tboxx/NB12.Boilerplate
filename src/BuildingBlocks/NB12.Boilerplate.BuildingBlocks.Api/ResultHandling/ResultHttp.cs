using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.BuildingBlocks.Api.ResultHandling
{
    public static class ResultHttp
    {
        public static IResult ToHttpResult(this Result result, HttpContext http)
        {
            if (result.IsSuccess)
                return Results.NoContent();

            var mapper = http.RequestServices.GetRequiredService<IProblemDetailsMapper>();
            var pd = mapper.FromErrors(http, result.Errors);
            return Results.Problem(pd);

            // Before ProblemDetails implementation
            // return Results.Problem(CreateProblemDetails(result.Errors));
        }

        public static IResult ToHttpResult<T>(this Result<T> result, HttpContext http, Func<T, IResult> onSuccess)
        {
            if (result.IsSuccess)
                return onSuccess(result.Value);

            var mapper = http.RequestServices.GetRequiredService<IProblemDetailsMapper>();
            var pd = mapper.FromErrors(http, result.Errors);
            return Results.Problem(pd);

            // Before ProblemDetails implementation
            // return Results.Problem(CreateProblemDetails(result.Errors));
        }

        //private static ProblemDetails CreateProblemDetails(IReadOnlyList<Error> errors)
        //{
        //    var primary = errors[0];

        //    var status = primary.Type switch
        //    {
        //        ErrorType.Validation => StatusCodes.Status400BadRequest,
        //        ErrorType.NotFound => StatusCodes.Status404NotFound,
        //        ErrorType.Conflict => StatusCodes.Status409Conflict,
        //        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        //        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        //        _ => StatusCodes.Status400BadRequest
        //    };

        //    var pd = new ProblemDetails
        //    {
        //        Status = status,
        //        Title = primary.Type.ToString(),
        //        Detail = primary.Message,
        //        Type = $"urn:error:{primary.Code}"
        //    };

        //    // Alle Fehler als Extension (API-Client kann sauber rendern)
        //    pd.Extensions["errors"] = errors.Select(e => new
        //    {
        //        e.Code,
        //        e.Message,
        //        Type = e.Type.ToString(),
        //        e.Metadata
        //    });

        //    return pd;
        //}
    }
}
