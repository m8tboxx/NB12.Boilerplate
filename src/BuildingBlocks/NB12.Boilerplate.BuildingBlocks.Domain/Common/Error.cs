using NB12.Boilerplate.BuildingBlocks.Domain.Enums;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Common
{
    public sealed record Error(
        string Code,
        string Message,
        ErrorType Type = ErrorType.Failure,
        IReadOnlyDictionary<string, object?>? Metadata = null)
    {
        public static Error Failure(string code, string message, IReadOnlyDictionary<string, object?>? meta = null)
        => new(code, message, ErrorType.Failure, meta);

        public static Error Validation(string code, string message, IReadOnlyDictionary<string, object?>? meta = null)
            => new(code, message, ErrorType.Validation, meta);

        public static Error NotFound(string code, string message, IReadOnlyDictionary<string, object?>? meta = null)
            => new(code, message, ErrorType.NotFound, meta);

        public static Error Conflict(string code, string message, IReadOnlyDictionary<string, object?>? meta = null)
            => new(code, message, ErrorType.Conflict, meta);

        public static Error Unauthorized(string code, string message, IReadOnlyDictionary<string, object?>? meta = null)
            => new(code, message, ErrorType.Unauthorized, meta);

        public static Error Forbidden(string code, string message, IReadOnlyDictionary<string, object?>? meta = null)
            => new(code, message, ErrorType.Forbidden, meta);
    }
}
