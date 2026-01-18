namespace NB12.Boilerplate.Modules.Auth.Application.Responses
{
    public sealed record RefreshTokenResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken);
}
