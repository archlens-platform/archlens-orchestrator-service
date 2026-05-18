using ArchLens.Orchestrator.Application.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ArchLens.Orchestrator.Tests.Application.Behaviors;

public class LoggingBehaviorTests
{
    private readonly ILogger<LoggingBehavior<TestRequest, TestResponse>> _logger;
    private readonly LoggingBehavior<TestRequest, TestResponse> _sut;

    public LoggingBehaviorTests()
    {
        _logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        _sut = new LoggingBehavior<TestRequest, TestResponse>(_logger);
    }

    [Fact]
    public async Task Handle_ShouldCallNextAndReturnResponse()
    {
        // Arrange
        var request = new TestRequest("value");
        var expectedResponse = new TestResponse("result");
        var next = Substitute.For<RequestHandlerDelegate<TestResponse>>();
        next().Returns(expectedResponse);

        // Act
        var result = await _sut.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await next.Received(1)();
    }

    [Fact]
    public async Task Handle_ShouldLogRequestName()
    {
        // Arrange
        var request = new TestRequest("value");
        var next = Substitute.For<RequestHandlerDelegate<TestResponse>>();
        next().Returns(new TestResponse("result"));

        // Act
        await _sut.Handle(request, next, CancellationToken.None);

        // Assert
        _logger.ReceivedWithAnyArgs(2).Log(
            default,
            default,
            default!,
            default,
            default!);
    }

    [Fact]
    public async Task Handle_WhenNextThrows_ShouldPropagateException()
    {
        // Arrange
        var request = new TestRequest("value");
        var next = Substitute.For<RequestHandlerDelegate<TestResponse>>();
        next().Returns<TestResponse>(_ => throw new InvalidOperationException("test error"));

        // Act
        var act = () => _sut.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("test error");
    }

    [Fact]
    public async Task Handle_ShouldPassThroughCancellationToken()
    {
        // Arrange
        var request = new TestRequest("value");
        using var cts = new CancellationTokenSource();
        var next = Substitute.For<RequestHandlerDelegate<TestResponse>>();
        next().Returns(new TestResponse("result"));

        // Act
        var result = await _sut.Handle(request, next, cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    public record TestRequest(string Value) : IRequest<TestResponse>;
    public record TestResponse(string Result);
}
