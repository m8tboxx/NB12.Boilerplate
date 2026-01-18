using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling
{
    public interface IProblemDetailsMapper
    {
        ProblemDetails FromErrors(HttpContext http, IReadOnlyList<Error> errors);
        ProblemDetails FromException(HttpContext http, Exception exception);
    }
}
