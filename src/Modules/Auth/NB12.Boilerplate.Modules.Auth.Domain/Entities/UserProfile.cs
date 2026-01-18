using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Domain.Ids;

namespace NB12.Boilerplate.Modules.Auth.Domain.Entities
{
    public sealed class UserProfile : AggregateRoot<UserProfileId>
    {
        private UserProfile() { } // EF

        private UserProfile(
            UserProfileId id,
            DateTime createdUtc,
            string? createdBy,
            string identityUserId,
            string firstName,
            string lastName,
            string email,
            string locale,
            DateTime? dateOfBirth)
            : base(id, createdUtc, createdBy)
        {
            IdentityUserId = identityUserId;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Locale = locale;
            DateOfBirth = dateOfBirth;
        }

        public static UserProfile Create(
            string identityUserId,
            string firstName,
            string lastName,
            string email,
            string locale,
            DateTime? dateOfBirth,
            DateTime utcNow,
            string? actor)
            => new(UserProfileId.New(), utcNow, actor, identityUserId, firstName, lastName, email, locale, dateOfBirth);

        public string IdentityUserId { get; private set; } = null!;
        public string FirstName { get; private set; } = null!;
        public string LastName { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string Locale { get; private set; } = null!;
        public DateTime? DateOfBirth { get; private set; }

        public string FullName => $"{FirstName} {LastName}";

        public void Update(string firstName, string lastName, string email, string locale, DateTime? dateOfBirth, DateTime utcNow, string? actor)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Locale = locale;
            DateOfBirth = dateOfBirth;

            SetModified(utcNow, actor);
        }
    }
}
