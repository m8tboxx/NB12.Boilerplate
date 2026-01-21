using Microsoft.Extensions.Logging;
using NB12.Boilerplate.BuildingBlocks.Application.Auditing;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.Modules.Audit.Domain.Entities;
using NB12.Boilerplate.Modules.Audit.Domain.Ids;
using NB12.Boilerplate.Modules.Audit.Infrastructure.Persistence;

namespace NB12.Boilerplate.Modules.Audit.Infrastructure.Services
{
    internal sealed class EfCoreErrorLogWriter(
        AuditDbContext db,
        ILogger<EfCoreErrorLogWriter> logger) : IErrorAuditWriter
    {
        public async Task WriteErrorAsync(
            ErrorAudit error,
            AuditContext context,
            CancellationToken ct = default)
        {
            try
            {
                var entity = new ErrorLog(
                    id: ErrorLogId.New(),
                    occurredAtUtc: context.OccurredAtUtc,
                    message: error.Message,
                    userId: context.UserId,
                    email: context.Email,
                    traceId: context.TraceId,
                    correlationId: context.CorrelationId,
                    exceptionType: error.ExceptionType,
                    stackTrace: error.StackTrace,
                    path: error.Path,
                    method: error.Method,
                    statusCode: error.StatusCode);

                db.ErrorLogs.Add(entity);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Audit darf nie die eigentliche Fehlerbehandlung killen
                logger.LogWarning(ex, "AUDIT: Failed to persist ErrorLog");
            }
        }
    }
}
