using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using System.Security.Claims;

namespace NB12.Boilerplate.BuildingBlocks.Infrastructure.Auth
{
    public sealed class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAuthenticated 
            => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

        public string? UserId
            => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");


        public string? Email 
            => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Email);
    }
}
