using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling
{
    public sealed class ProblemDetailsAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly IProblemDetailsMapper _mapper;

        public ProblemDetailsAuthorizationMiddlewareResultHandler(
            IProblemDetailsService problemDetailsService,
            IProblemDetailsMapper mapper)
        {
            _problemDetailsService = problemDetailsService;
            _mapper = mapper;
        }

        public async Task HandleAsync(
            RequestDelegate next,
            HttpContext http,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult authorizeResult)
        {
            // Let default handler set headers (WWW-Authenticate etc.)
            await _defaultHandler.HandleAsync(next, http, policy, authorizeResult);

            if (http.Response.HasStarted)
                return;

            if (authorizeResult.Challenged)
            {
                var pd = _mapper.FromErrors(http, new[]
                {
                Error.Unauthorized("auth.not_authenticated", "Not authenticated.")
            });

                http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await _problemDetailsService.TryWriteAsync(new() { HttpContext = http, ProblemDetails = pd });
            }
            else if (authorizeResult.Forbidden)
            {
                var pd = _mapper.FromErrors(http, new[]
                {
                Error.Forbidden("auth.forbidden", "Forbidden.")
            });

                http.Response.StatusCode = StatusCodes.Status403Forbidden;
                await _problemDetailsService.TryWriteAsync(new() { HttpContext = http, ProblemDetails = pd });
            }
        }
    }
}
