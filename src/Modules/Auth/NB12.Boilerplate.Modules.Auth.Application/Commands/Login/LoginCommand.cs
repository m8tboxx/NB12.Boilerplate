using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Application.Responses;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.Login
{
    public sealed record LoginCommand(string Email, string Password)
        : IRequest<Result<LoginResponse>>;
}
