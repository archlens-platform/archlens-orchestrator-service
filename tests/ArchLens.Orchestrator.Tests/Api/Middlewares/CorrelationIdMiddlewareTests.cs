using ArchLens.Orchestrator.Api.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace ArchLens.Orchestrator.Tests.Api.Middlewares;

public class CorrelationIdMiddlewareTests
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    [Fact]
    public async Task InvokeAsync_WithExistingCorrelationId_ShouldPreserveIt()
    {
        // Arrange
        var existingCorrelationId = Guid.NewGuid().ToString();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdHeader] = existingCorrelationId;

        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.Headers[CorrelationIdHeader].ToString().Should().Be(existingCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_WithoutCorrelationId_ShouldGenerateNew()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[CorrelationIdHeader].ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyCorrelationId_ShouldGenerateNew()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdHeader] = "";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers[CorrelationIdHeader].ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_WithWhitespaceCorrelationId_ShouldGenerateNew()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdHeader] = "   ";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseHeader = context.Response.Headers[CorrelationIdHeader].ToString();
        responseHeader.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetCorrelationIdInItems()
    {
        // Arrange
        var existingCorrelationId = Guid.NewGuid().ToString();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdHeader] = existingCorrelationId;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items[CorrelationIdHeader].Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_GeneratedId_ShouldBeValidGuid()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseHeader = context.Response.Headers[CorrelationIdHeader].ToString();
        Guid.TryParse(responseHeader, out _).Should().BeTrue();
    }
}
