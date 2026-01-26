using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NB12.Boilerplate.BuildingBlocks.Api.Modularity;
using NB12.Boilerplate.Modules.Audit.Api.Endpoints;

namespace NB12.Boilerplate.Modules.Audit.Api
{
    public sealed class AuditAdminEndpointsModule : IEndpointModule
    {
        public string Name => "AuditAdminModule";
        public void MapEndpoints(IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("/api/audit")
                .WithTags("Audit");

            api.MapAuditAdminEndpoints();
        }
    }
}
