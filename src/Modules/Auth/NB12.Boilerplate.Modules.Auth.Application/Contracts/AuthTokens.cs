namespace NB12.Boilerplate.Modules.Auth.Application.Contracts
{
    public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);
}
