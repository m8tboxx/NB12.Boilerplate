using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Models;
using System.Security.Claims;

namespace NB12.Boilerplate.Modules.Auth.Infrastructure.Security
{
    public static class SecurityStampValidator
    {
        public const string StampClaim = "sst";

        public static JwtBearerEvents CreateEvents()
            => new()
            {
                OnTokenValidated = async ctx =>
                {
                    var userId = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? ctx.Principal?.FindFirstValue("sub");

                    var stamp = ctx.Principal?.FindFirstValue(StampClaim);

                    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(stamp))
                    {
                        ctx.Fail("Missing required claims.");
                        return;
                    }

                    var um = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var user = await um.FindByIdAsync(userId);

                    if (user is null || !string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal))
                        ctx.Fail("Token invalid (security stamp mismatch).");
                }
            };
    }
}
