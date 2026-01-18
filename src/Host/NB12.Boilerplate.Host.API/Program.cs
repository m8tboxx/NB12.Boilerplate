using MediatR;
using NB12.Boilerplate.BuildingBlocks.Api.Extensions;
using NB12.Boilerplate.BuildingBlocks.Application.Behaviors;
using NB12.Boilerplate.BuildingBlocks.Application.Modularity;
using NB12.Boilerplate.BuildingBlocks.Application.Validation;
using NB12.Boilerplate.Host.API.Modules;
using NB12.Boilerplate.Host.API.OpenApi;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence.Seeding;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Module Loading
var serviceModules = ModuleRegistration.ServiceModules();
var endpointModules = ModuleRegistration.EndpointModules();

// Module DI
foreach(var module in serviceModules)
{
    module.AddModule(builder.Services, builder.Configuration);
}

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(ModuleCatalog.GetApplicationAssemblies(serviceModules));
});

// Global Pipeline
builder.Services.AddValidatorsFromAssemblies(ModuleCatalog.GetApplicationAssemblies(serviceModules));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
// builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
// builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiInfoTransformer>();
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerAuthOperationTransformer>();
});

builder.Services.AddApiBuildingBlocks();

var app = builder.Build();

app.UseApiBuildingBlocks();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<AuthSeeder>();
    await seeder.SeedAsync();
}

foreach (var module in endpointModules)
{
    module.MapEndpoints(app);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference("/docs", options =>
    {
        options
        .WithTheme(ScalarTheme.BluePlanet)
        .WithTitle("NB12 Boilerplate API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    }).AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.Run();

