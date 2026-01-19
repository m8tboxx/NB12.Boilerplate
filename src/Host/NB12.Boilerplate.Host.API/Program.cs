using MediatR;
using Microsoft.AspNetCore.Authorization;
using NB12.Boilerplate.BuildingBlocks.Api.Extensions;
using NB12.Boilerplate.BuildingBlocks.Application.Behaviors;
using NB12.Boilerplate.BuildingBlocks.Application.Modularity;
using NB12.Boilerplate.BuildingBlocks.Application.Validation;
using NB12.Boilerplate.BuildingBlocks.Infrastructure;
using NB12.Boilerplate.Host.API.Modules;
using NB12.Boilerplate.Host.API.OpenApi;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence.Seeding;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext());

// Cross-cutting infrastructure (CurrentUser, dynamic permission policies, etc.)
builder.Services.AddInfrastructureBuildingBlocks();

builder.Services.AddSingleton<ModuleCatalog>();

// Module Loading
var serviceModules = ModuleRegistration.ServiceModules();
var endpointModules = ModuleRegistration.EndpointModules();

var moduleAssemblies = serviceModules
    .Select(m => m.ApplicationAssembly)
    .Distinct()
    .ToArray();

// Module DI
foreach(var module in serviceModules)
{
    module.AddModule(builder.Services, builder.Configuration);
}

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(moduleAssemblies);
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Global Pipeline
builder.Services.AddValidatorsFromAssemblies(moduleAssemblies);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
// builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiInfoTransformer>();
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerAuthOperationTransformer>();
});

builder.Services.AddApiBuildingBlocks();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseApiBuildingBlocks();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

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
        .WithTheme(ScalarTheme.DeepSpace)
        .WithTitle("NB12 Boilerplate API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    }).AllowAnonymous();

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<AuthSeeder>();
    await seeder.SeedAsync();
}

app.Run();

