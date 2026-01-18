using Microsoft.AspNetCore.Identity;
using NB12.Boilerplate.Modules.Auth.Domain.Entities;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Models
{
    public sealed class ApplicationUser : IdentityUser
    {
        public UserProfile? UserProfile { get; set; }
    }
}
