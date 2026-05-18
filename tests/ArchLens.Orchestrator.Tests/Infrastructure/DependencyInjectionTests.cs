using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.Orchestrator.Infrastructure;
using ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Context;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLens.Orchestrator.Tests.Infrastructure;

public class DependencyInjectionTests
{
    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest"
            })
            .Build();
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterDbContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateConfiguration();

        services.AddInfrastructure(config);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(SagaDbContext));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterSagaStateRepository()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateConfiguration();

        services.AddInfrastructure(config);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISagaStateRepository));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterMassTransit()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateConfiguration();

        services.AddInfrastructure(config);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IBusControl));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddInfrastructure_ShouldReturnSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateConfiguration();

        var result = services.AddInfrastructure(config);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddInfrastructure_WithMissingPostgresConnection_ShouldThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest"
            })
            .Build();

        var act = () => services.AddInfrastructure(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PostgreSQL*");
    }

    [Fact]
    public void AddInfrastructure_WithMissingRabbitMqHost_ShouldThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=test",
            })
            .Build();

        var act = () => services.AddInfrastructure(config);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterDbContextAsDbContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = CreateConfiguration();

        services.AddInfrastructure(config);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContext));
        descriptor.Should().NotBeNull();
    }
}
