using NB12.Boilerplate.Modules.Audit.Domain.Ids;

namespace NB12.Boilerplate.Modules.Audit.Domain.Entities
{
    public sealed class ErrorLog
    {
        public ErrorLog(
            ErrorLogId id,
            DateTime occurredAtUtc,
            string message,
            string? userId,
            string? email,
            string? traceId,
            string? correlationId,
            string? exceptionType,
            string? stackTrace,
            string? path,
            string? method,
            int? statusCode)
        {
            Id = id;
            OccurredAtUtc = occurredAtUtc;
            Message = message;
            UserId = userId;
            Email = email;
            TraceId = traceId;
            CorrelationId = correlationId;
            ExceptionType = exceptionType;
            StackTrace = stackTrace;
            Path = path;
            Method = method;
            StatusCode = statusCode;
        }

        public ErrorLogId Id { get; private set; }
        public DateTime OccurredAtUtc { get; private set; }

        public string? UserId { get; private set; }
        public string? Email { get; private set; }
        public string? TraceId { get; private set; }
        public string? CorrelationId { get; private set; }

        public string Message { get; private set; } = default!;
        public string? ExceptionType { get; private set; }
        public string? StackTrace { get; private set; }

        public string? Path { get; private set; }
        public string? Method { get; private set; }
        public int? StatusCode { get; private set; }
    }
}
