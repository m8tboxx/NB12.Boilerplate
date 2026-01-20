using NB12.Boilerplate.BuildingBlocks.Domain.Abstractions;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Ids
{
    public sealed record OutboxMessageId(Guid Value) : StronglyTypedId<Guid>(Value)
    {
        public static OutboxMessageId New()
        {
            var id = Guid.NewGuid();
            ThrowIfDefault(id, nameof(id));
            return new OutboxMessageId(id);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
