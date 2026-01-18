using MediatR;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.RenameRole
{
    public sealed record RenameRoleCommand(string RoleId, string NewName) : IRequest<Result>;
}
