namespace NB12.Boilerplate.Modules.Auth.Application.Responses
{
    public sealed record MeResponse(string UserId, string? Email, string DisplayName, string? Locale, string[] Roles);
}
