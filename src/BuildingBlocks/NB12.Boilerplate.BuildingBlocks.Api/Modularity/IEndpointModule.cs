using Microsoft.AspNetCore.Routing;

namespace NB12.Boilerplate.BuildingBlocks.Api.Modularity
{
    public interface IEndpointModule
    {
        string Name { get; }
        void MapEndpoints(IEndpointRouteBuilder endpoints);
    }
}
