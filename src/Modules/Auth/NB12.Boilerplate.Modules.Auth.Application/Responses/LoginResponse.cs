namespace NB12.Boilerplate.Modules.Auth.Application.Responses
{
    public sealed record LoginResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken);
}
