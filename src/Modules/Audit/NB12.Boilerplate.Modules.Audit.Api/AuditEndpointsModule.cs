using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NB12.Boilerplate.BuildingBlocks.Web.Modularity;
using NB12.Boilerplate.Modules.Audit.Api.Endpoints;

namespace NB12.Boilerplate.Modules.Audit.Api
{
    public sealed class AuditEndpointsModule : IModuleEndpoints
    {
        public string Name => "AuditModule";

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/audit")
                .WithTags("Audit");

            group.MapAuditReadEndpoints();
        }
    }
}
