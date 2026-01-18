using Microsoft.AspNetCore.Routing;

namespace NB12.Boilerplate.BuildingBlocks.Application.Interfaces
{
    public interface IModuleEndpoints
    {
        string Name { get; }
        void MapEndpoints(IEndpointRouteBuilder endpoints);
    }
}
