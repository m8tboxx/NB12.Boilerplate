using Microsoft.AspNetCore.Authorization;
using NB12.Boilerplate.BuildingBlocks.Api.Extensions;
using NB12.Boilerplate.BuildingBlocks.Api.Middleware;
using NB12.Boilerplate.BuildingBlocks.Api.Middleware.ETag;
using NB12.Boilerplate.BuildingBlocks.Application.Behaviors;
using NB12.Boilerplate.BuildingBlocks.Application.Eventing;
using NB12.Boilerplate.BuildingBlocks.Application.Modularity;
using NB12.Boilerplate.BuildingBlocks.Application.Validation;
using NB12.Boilerplate.BuildingBlocks.Infrastructure;
using NB12.Boilerplate.Host.API.Modules;
using NB12.Boilerplate.Host.API.OpenApi;
using NB12.Boilerplate.Modules.Auth.Infrastructure.Persistence.Seeding;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging;
using NB12.Boilerplate.BuildingBlocks.Application.Messaging.Abstractions;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "Frontend";

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

builder.Services.AddMessaging(moduleAssemblies);


builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Global Pipeline
builder.Services.AddValidatorsFromAssemblies(moduleAssemblies);
builder.Services.AddDomainEventing(moduleAssemblies);

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiInfoTransformer>();
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerAuthOperationTransformer>();
});

builder.Services.AddApiBuildingBlocks();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: "NB12.Boilerplate.Host.API",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(o =>
            {
                o.RecordException = true;
                o.EnrichWithHttpRequest = (activity, request) =>
                {
                    if (request.Headers.TryGetValue("X-Correlation-Id", out var cid))
                        activity.SetTag("correlation_id", cid.ToString());
                };
            })
            .AddHttpClientInstrumentation(o => o.RecordException = true)
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.EnrichWithIDbCommand = (activity, command) =>
                {
                    activity.SetTag("db.statement", null);
                    activity.SetTag("db.query.text", null);
                    activity.SetTag("db.command_type", command.CommandType.ToString());
                };
            })
            .AddOtlpExporter();
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins("https://localhost") // wichtig: exakt Scheme+Host, ohne Port
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "If-None-Match", "X-Correlation-Id")
            .WithExposedHeaders("ETag", "X-Correlation-Id") // sonst kann Nuxt ETag nicht lesen
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();
app.UseApiBuildingBlocks();
app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseETag();

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

