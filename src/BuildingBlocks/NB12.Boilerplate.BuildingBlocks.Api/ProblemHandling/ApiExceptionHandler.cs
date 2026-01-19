using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;

namespace NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling
{
    public sealed class ApiExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly IProblemDetailsMapper _mapper;
        private readonly ILogger<ApiExceptionHandler> _logger;
        //private readonly IAuditStore _auditStore;
        //private readonly IAuditContextAccessor _auditCtx;
        private readonly IServiceScopeFactory _scopeFactory;

        public ApiExceptionHandler(
            IProblemDetailsService problemDetailsService,
            IProblemDetailsMapper mapper,
            ILogger<ApiExceptionHandler> logger,
            IServiceScopeFactory scopeFactory)
            //IAuditStore auditStore,
            //IAuditContextAccessor auditCtx)
        {
            _problemDetailsService = problemDetailsService;
            _mapper = mapper;
            _logger = logger;
            _scopeFactory = scopeFactory;
            //_auditStore = auditStore;
            //_auditCtx = auditCtx;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
        {
            _logger.LogError(exception, "Unhandled exception");

            using var scope = _scopeFactory.CreateScope();
            var _auditStore = scope.ServiceProvider.GetRequiredService<IAuditStore>();
            var _auditCtx = scope.ServiceProvider.GetRequiredService<IAuditContextAccessor>();

            var pd = _mapper.FromException(http, exception);
            http.Response.StatusCode = pd.Status ?? StatusCodes.Status500InternalServerError;

            try
            {
                var ctx = _auditCtx.GetCurrent();
                await _auditStore.WriteErrorAsync(
                    new ErrorAudit(
                        Message: exception.Message,
                        ExceptionType: exception.GetType().FullName,
                        StackTrace: exception.ToString(),
                        Path: http.Request.Path,
                        Method: http.Request.Method,
                        StatusCode: http.Response.StatusCode),
                    ctx,
                    ct);
            }
            catch (Exception ex)
            {
                // Audit darf nie die Fehlerbehandlung killen
                _logger.LogWarning(ex, "Failed to write error audit log");
            }

            return await _problemDetailsService.TryWriteAsync(new()
            {
                HttpContext = http,
                ProblemDetails = pd
            });
        }
    }
}
