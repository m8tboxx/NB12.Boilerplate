using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NB12.Boilerplate.BuildingBlocks.Web.Modularity;
using NB12.Boilerplate.Modules.Auth.Api.Endpoints;

namespace NB12.Boilerplate.Modules.Auth.Api
{
    public sealed class AuthAdminEndpointsModule : IModuleEndpoints
    {
        public string Name => "AuthAdmin";

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/auth/admin")
                .WithTags("AuthAdmin");

            group.MapAuthAdminEndpoints();
        }
    }
}
