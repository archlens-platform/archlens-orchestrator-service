using System.Text;
using ArchLens.Orchestrator.Api.Configurations;
using ArchLens.Orchestrator.Api.ExceptionHandlers;
using ArchLens.Orchestrator.Api.Middlewares;
using ArchLens.Orchestrator.Application;
using ArchLens.Orchestrator.Infrastructure;
using ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilogLogging();
    builder.AddOpenTelemetryObservability();
    builder.AddRateLimiting();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Informe o token JWT: Bearer {token}"
        });
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                []
            }
        });
    });
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL configuration is required."),
            name: "postgresql",
            tags: ["db"]);
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var jwtSection = builder.Configuration.GetRequiredSection("Jwt");
    var jwtKey = jwtSection["Key"]
        ?? throw new InvalidOperationException("Configuration 'Jwt:Key' is required");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"] ?? "archlens-auth",
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"] ?? "archlens-services",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = true,
            };
        });
    builder.Services.AddAuthorization();

    var app = builder.Build();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SagaDbContext>();
        await db.Database.MigrateAsync();
    }

    app.UseExceptionHandler();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

namespace ArchLens.Orchestrator.Api
{
    public partial class Program;
}
