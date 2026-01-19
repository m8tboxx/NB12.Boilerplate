using NB12.Boilerplate.BuildingBlocks.Domain.Abstractions;

namespace NB12.Boilerplate.Modules.Audit.Domain.Ids
{
    public sealed record ErrorLogId(Guid Value) : StronglyTypedId<Guid>(Value)
    {
        public static ErrorLogId New()
        {
            var id = Guid.NewGuid();
            ThrowIfDefault(id, nameof(id));
            return new ErrorLogId(id);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
