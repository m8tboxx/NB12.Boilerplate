using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling
{
    public sealed class ApiExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly IProblemDetailsMapper _mapper;
        private readonly ILogger<ApiExceptionHandler> _logger;

        public ApiExceptionHandler(
            IProblemDetailsService problemDetailsService,
            IProblemDetailsMapper mapper,
            ILogger<ApiExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _mapper = mapper;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
        {
            _logger.LogError(exception, "Unhandled exception");

            var pd = _mapper.FromException(http, exception);

            http.Response.StatusCode = pd.Status ?? StatusCodes.Status500InternalServerError;

            return await _problemDetailsService.TryWriteAsync(new()
            {
                HttpContext = http,
                ProblemDetails = pd
            });
        }
    }
}
