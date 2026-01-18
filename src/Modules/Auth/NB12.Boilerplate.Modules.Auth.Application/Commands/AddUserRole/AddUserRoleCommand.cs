using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.AddUserRole
{
    public sealed record AddUserRoleCommand(string UserId, string RoleName) : IRequest<Result>;
}
