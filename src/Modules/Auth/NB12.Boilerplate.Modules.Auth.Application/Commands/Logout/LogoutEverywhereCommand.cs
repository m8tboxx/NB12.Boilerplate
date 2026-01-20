using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.Logout
{
    public sealed record LogoutEverywhereCommand(string UserId) : IRequest<Result>;
}
