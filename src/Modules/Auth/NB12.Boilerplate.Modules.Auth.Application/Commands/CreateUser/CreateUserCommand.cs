using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.CreateUser
{
    public sealed record CreateUserCommand(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Locale,
        DateTime? DateOfBirth)
        : IRequest<Result<string>>;
}
