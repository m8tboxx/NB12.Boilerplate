using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Domain.Ids;

namespace NB12.Boilerplate.Modules.Auth.Domain.Events
{
    public sealed class UserProfileCreatedDomainEvent(
    UserProfileId profileId,
    string identityUserId,
    string email) : DomainEventBase
    {
        public UserProfileId ProfileId { get; } = profileId;
        public string IdentityUserId { get; } = identityUserId;
        public string Email { get; } = email;
    }
}
