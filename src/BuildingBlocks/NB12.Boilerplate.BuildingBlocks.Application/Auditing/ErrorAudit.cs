namespace NB12.Boilerplate.BuildingBlocks.Application.Auditing
{
    public sealed record ErrorAudit(
        string Message,
        string? ExceptionType,
        string? StackTrace,
        string? Path,
        string? Method,
        int? StatusCode);
}
