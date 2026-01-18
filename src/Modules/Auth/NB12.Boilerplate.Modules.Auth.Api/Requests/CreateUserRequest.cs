namespace NB12.Boilerplate.Modules.Auth.Api.Requests
{
    public sealed record CreateUserRequest(
            string Email,
            string Password,
            string FirstName,
            string LastName,
            string Locale,
            DateTime? DateOfBirth);
}
