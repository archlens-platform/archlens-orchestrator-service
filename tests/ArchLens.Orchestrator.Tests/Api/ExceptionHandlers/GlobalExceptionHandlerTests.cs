using System.Net;
using ArchLens.Orchestrator.Api.ExceptionHandlers;
using ArchLens.SharedKernel.Domain;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ArchLens.Orchestrator.Tests.Api.ExceptionHandlers;

public class GlobalExceptionHandlerTests
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly GlobalExceptionHandler _sut;

    public GlobalExceptionHandlerTests()
    {
        _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        _sut = new GlobalExceptionHandler(_logger);
    }

    [Fact]
    public async Task TryHandleAsync_WithValidationException_ShouldReturnBadRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var failures = new List<ValidationFailure>
        {
            new("Property", "Error message")
        };
        var exception = new ValidationException(failures);

        // Act
        var handled = await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_WithDomainException_ShouldReturnUnprocessableEntity()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new TestDomainException("TEST_CODE", "Domain error occurred");

        // Act
        var handled = await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task TryHandleAsync_WithUnhandledException_ShouldReturnInternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new InvalidOperationException("Unexpected error");

        // Act
        var handled = await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task TryHandleAsync_WithUnhandledException_ShouldLogError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new InvalidOperationException("Unexpected error");

        // Act
        await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        _logger.ReceivedWithAnyArgs(1).Log(
            default,
            default,
            default!,
            default,
            default!);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act & Assert - all exception types should return true
        var result1 = await _sut.TryHandleAsync(context, new ValidationException(new List<ValidationFailure>()), CancellationToken.None);
        result1.Should().BeTrue();

        context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var result2 = await _sut.TryHandleAsync(context, new TestDomainException("CODE", "msg"), CancellationToken.None);
        result2.Should().BeTrue();

        context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var result3 = await _sut.TryHandleAsync(context, new Exception("generic"), CancellationToken.None);
        result3.Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_WithNullReferenceException_ShouldReturnInternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new NullReferenceException("Object reference not set");

        // Act
        var handled = await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    private sealed class TestDomainException : DomainException
    {
        public TestDomainException(string code, string message) : base(code, message) { }
    }
}
