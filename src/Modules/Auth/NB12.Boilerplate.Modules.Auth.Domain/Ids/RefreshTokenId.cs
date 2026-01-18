using NB12.Boilerplate.BuildingBlocks.Domain.Abstractions;

namespace NB12.Boilerplate.Modules.Auth.Domain.Ids
{
    public sealed record RefreshTokenId(Guid Value) : StronglyTypedId<Guid>(Value)
    {
        public static RefreshTokenId New()
        {
            var id = Guid.NewGuid();
            ThrowIfDefault(id, nameof(id));
            return new RefreshTokenId(id);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
