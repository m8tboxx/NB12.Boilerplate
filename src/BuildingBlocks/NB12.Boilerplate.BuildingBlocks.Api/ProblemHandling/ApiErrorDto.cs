using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling
{
    public sealed record ApiErrorDto(
    string Code,
    string Message,
    string Type,
    IReadOnlyDictionary<string, object?>? Metadata)
    {
        public static ApiErrorDto From(Error e)
            => new(e.Code, e.Message, e.Type.ToString(), e.Metadata);
    }
}
