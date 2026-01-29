using NB12.Boilerplate.BuildingBlocks.Domain.Abstractions;

namespace NB12.Boilerplate.BuildingBlocks.Application.Ids
{
    public sealed record InboxMessageId(Guid Value) : StronglyTypedId<Guid>(Value)
    {
        public static InboxMessageId New()
        {
            var id = Guid.NewGuid();
            ThrowIfDefault(id, nameof(id));
            return new InboxMessageId(id);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static InboxMessageId Parse(string s, IFormatProvider? provider)
        {
            if (TryParse(s, provider, out var id))
                return id;

            throw new FormatException($"Invalid {nameof(InboxMessageId)}: '{s}'.");
        }

        public static bool TryParse(string? s, IFormatProvider? provider, out InboxMessageId result)
        {
            result = default!;

            if (string.IsNullOrWhiteSpace(s))
                return false;

            // Route values come as strings; guid constraint is fine but not required.
            if (!Guid.TryParse(s, out var guid))
                return false;

            result = new InboxMessageId(guid);
            return true;
        }
    }
}
