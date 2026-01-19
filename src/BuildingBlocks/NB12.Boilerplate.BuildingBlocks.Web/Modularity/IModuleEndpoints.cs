using Microsoft.AspNetCore.Routing;

namespace NB12.Boilerplate.BuildingBlocks.Web.Modularity
{
    public interface IModuleEndpoints
    {
        string Name { get; }
        void MapEndpoints(IEndpointRouteBuilder endpoints);
    }
}
