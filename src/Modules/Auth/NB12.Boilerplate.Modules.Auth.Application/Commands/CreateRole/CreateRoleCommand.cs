using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.CreateRole
{
    public sealed record CreateRoleCommand(string Name) : IRequest<Result<string>>;
}
