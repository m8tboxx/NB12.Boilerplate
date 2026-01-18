using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NB12.Boilerplate.BuildingBlocks.Api.ProblemHandling;
using NB12.Boilerplate.BuildingBlocks.Api.ResultHandling;
using NB12.Boilerplate.BuildingBlocks.Domain.Common;
using NB12.Boilerplate.Modules.Auth.Api.Cookies;
using NB12.Boilerplate.Modules.Auth.Application.Commands.Login;
using NB12.Boilerplate.Modules.Auth.Application.Commands.Logout;
using NB12.Boilerplate.Modules.Auth.Application.Commands.Refresh;
using NB12.Boilerplate.Modules.Auth.Application.Queries.MeQuery;
using NB12.Boilerplate.Modules.Auth.Application.Security;
using System.Security.Claims;

namespace NB12.Boilerplate.Modules.Auth.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
        {
            group.MapPost("/login", Login).AllowAnonymous();
            group.MapPost("/refresh", Refresh).AllowAnonymous();
            group.MapPost("/logout", Logout).RequireAuthorization();
            group.MapPost("/logoutEverywhere", LogoutEverywhere).RequireAuthorization();
            group.MapGet("/me", GetMe).RequireAuthorization(Permissions.Auth.MeRead);

            return group;
        }

        private static async Task<IResult> Login(
            LoginRequest request,
            ISender sender,
            RefreshTokenCookies cookies,
            HttpContext http,
            CancellationToken ct)
        {
            var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);

            return result.ToHttpResult(http, value =>
            {
                cookies.Set(http.Response, value.RefreshToken);
                return Results.Ok(new { value.AccessToken, value.AccessTokenExpiresAt, value.RefreshToken });
            });
        }

        private static async Task<IResult> Refresh(
            ISender sender,
            RefreshTokenCookies cookies,
            HttpContext http,
            CancellationToken ct)
        {
            var rt = cookies.Get(http.Request);

            if (string.IsNullOrWhiteSpace(rt))
            {
                var mapper = http.RequestServices.GetRequiredService<IProblemDetailsMapper>();
                var pd = mapper.FromErrors(http, new[]
                {
                    Error.Unauthorized("auth.refresh_token_missing", "Missing refresh token.")
                });

                return Results.Problem(pd);
            }

            var result = await sender.Send(new RefreshTokenCommand(rt), ct);

            return result.ToHttpResult(http, value =>
            {
                cookies.Set(http.Response, value.RefreshToken);
                return Results.Ok(new { value.AccessToken, value.AccessTokenExpiresAt, value.RefreshToken });
            });
        }

        private static async Task<IResult> Logout(
            ISender sender,
            RefreshTokenCookies cookies,
            HttpContext http,
            CancellationToken ct)
        {
            var refreshToken = cookies.Get(http.Request);
            var result = await sender.Send(new LogoutCommand(refreshToken), ct);
            cookies.Clear(http.Response);
            return result.ToHttpResult(http);
        }

        private static async Task<IResult> LogoutEverywhere(
            ISender sender, 
            RefreshTokenCookies cookies, 
            HttpContext http, 
            CancellationToken ct)
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue("sub")
                ?? "";

            var result = await sender.Send(new LogoutEverywhereCommand(userId), ct);

            if (result.IsSuccess)
                cookies.Clear(http.Response);

            return result.ToHttpResult(http);
        }

        private static async Task<IResult> GetMe(ISender sender, HttpContext http, CancellationToken ct)
        {
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? http.User.FindFirstValue("sub")
                         ?? "";

            var result = await sender.Send(new GetMeQuery(userId), ct);
            return result.ToHttpResult(http, x => Results.Ok(x));
        }
    }
}
