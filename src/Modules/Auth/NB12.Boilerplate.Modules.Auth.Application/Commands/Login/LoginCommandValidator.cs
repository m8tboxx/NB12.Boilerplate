using FluentValidation;

namespace NB12.Boilerplate.Modules.Auth.Application.Commands.Login
{
    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        // TODO: Delete?
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
        }
    }
}
