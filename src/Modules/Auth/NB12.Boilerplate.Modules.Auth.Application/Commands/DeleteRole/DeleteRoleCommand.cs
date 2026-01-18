using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.DeleteRole
{
    public sealed record DeleteRoleCommand(string RoleId) : IRequest<Result>;
}
