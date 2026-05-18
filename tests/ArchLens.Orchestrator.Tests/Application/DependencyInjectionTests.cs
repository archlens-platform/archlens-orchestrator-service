using ArchLens.Orchestrator.Application;
using ArchLens.Orchestrator.Application.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLens.Orchestrator.Tests.Application;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ShouldRegisterMediatR()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddApplication();

        // Assert
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_ShouldRegisterValidationBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddApplication();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(ValidationBehavior<,>));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_ShouldRegisterLoggingBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddApplication();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType == typeof(LoggingBehavior<,>));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddApplication();

        // Assert
        result.Should().BeSameAs(services);
    }
}
