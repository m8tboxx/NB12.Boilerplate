using NB12.Boilerplate.Modules.Audit.Domain.Ids;

namespace NB12.Boilerplate.Modules.Audit.Application.Responses
{
    public sealed record ErrorLogDto(
        ErrorLogId Id,
        DateTime OccurredAtUtc,
        string? UserId,
        string? Email,
        string? TraceId,
        string? CorrelationId,
        string Message,
        string? ExceptionType,
        string? StackTrace,
        string? Path,
        string? Method,
        int? StatusCode);
}
