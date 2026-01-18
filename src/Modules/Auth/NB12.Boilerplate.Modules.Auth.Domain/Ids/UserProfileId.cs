using NB12.Boilerplate.BuildingBlocks.Domain.Abstractions;

namespace NB12.Boilerplate.Modules.Auth.Domain.Ids
{
    public sealed record UserProfileId(Guid Value) : StronglyTypedId<Guid>(Value)
    {
        public static UserProfileId New()
        {
            var id = Guid.NewGuid();
            ThrowIfDefault(id, nameof(id));
            return new UserProfileId(id);
        }
    }
}
