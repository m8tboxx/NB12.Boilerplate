using Microsoft.AspNetCore.Builder;

namespace NB12.Boilerplate.BuildingBlocks.Api.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication UseApiBuildingBlocks(this WebApplication app)
        {
            app.UseExceptionHandler();
            app.UseStatusCodeProblemDetails(); // 404/405/415 etc.

            return app;
        }
    }
}
