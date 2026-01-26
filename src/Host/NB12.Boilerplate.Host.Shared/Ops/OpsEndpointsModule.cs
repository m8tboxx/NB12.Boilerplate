using Microsoft.AspNetCore.Routing;
using NB12.Boilerplate.BuildingBlocks.Api.Modularity;

namespace NB12.Boilerplate.Host.Shared.Ops
{
    public sealed class OpsEndpointsModule : IEndpointModule
    {
        public string Name => "OpsModule";

        public void MapEndpoints(IEndpointRouteBuilder app)
        {
            OpsEndpoints.MapOpsEndpoints(app);
        }
    }
}
