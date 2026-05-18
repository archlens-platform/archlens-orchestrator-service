using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Context;
using ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Repositories.SagaStateRepositories;
using ArchLens.Orchestrator.Infrastructure.Saga;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLens.Orchestrator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddMessaging(configuration);
        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is required");

        services.AddDbContext<SagaDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(SagaDbContext).Assembly.FullName)));

        services.AddScoped<ISagaStateRepository, SagaStateRepository>();
    }

    private static void AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is required");
        var rabbitSection = configuration.GetRequiredSection("RabbitMQ");
        var host = rabbitSection["Host"] ?? throw new InvalidOperationException("Configuration 'RabbitMQ:Host' is required");
        var username = rabbitSection["Username"] ?? throw new InvalidOperationException("Configuration 'RabbitMQ:Username' is required");
        var password = rabbitSection["Password"] ?? throw new InvalidOperationException("Configuration 'RabbitMQ:Password' is required");

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            bus.AddSagaStateMachine<AnalysisSagaStateMachine, AnalysisSagaState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;

                    r.AddDbContext<DbContext, SagaDbContext>((_, optionsBuilder) =>
                    {
                        optionsBuilder.UseNpgsql(connectionString, npgsql =>
                            npgsql.MigrationsAssembly(typeof(SagaDbContext).Assembly.FullName));
                    });
                });

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, "/", h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)));

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
