using Microsoft.AspNetCore.Http;
using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using System.Diagnostics;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Auditing
{
    public sealed class DefaultAuditContextAccessor : IAuditContextAccessor
    {
        private readonly ICurrentUser _currentUser;
        private readonly IHttpContextAccessor _http;

        public DefaultAuditContextAccessor(ICurrentUser currentUser, IHttpContextAccessor http)
        {
            _currentUser = currentUser;
            _http = http;
        }

        public AuditContext GetCurrent()
        {
            var now = DateTime.UtcNow;

            // Distributed tracing first
            var traceId = Activity.Current?.TraceId.ToString()
                          ?? _http.HttpContext?.TraceIdentifier;

            // CorrelationId: Bei Einführung der Middleware, hier setzen.
            var correlationId =
                _http.HttpContext?.Request.Headers.TryGetValue("X-Correlation-Id", out var cid) == true
                    ? cid.ToString()
                    : null;

            return new AuditContext(
                OccurredAtUtc: now,
                UserId: _currentUser.UserId,
                Email: _currentUser.Email,
                TraceId: traceId,
                CorrelationId: correlationId);
        }
    }
}
