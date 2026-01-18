using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NB12.Boilerplate.BuildingBlocks.Application.Interfaces;
using NB12.Boilerplate.Modules.Auth.Api.Endpoints;

namespace NB12.Boilerplate.Modules.Auth.Api
{
    public sealed class AuthEndpointsModule : IModuleEndpoints
    {
        public string Name => "AuthModule";

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/auth")
            .WithTags("Auth");

            group.MapAuthEndpoints();
        }
    }
}
