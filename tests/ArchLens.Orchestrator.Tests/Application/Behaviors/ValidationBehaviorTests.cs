using ArchLens.Orchestrator.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;

namespace ArchLens.Orchestrator.Tests.Application.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("valid");
        var expectedResponse = new TestResponse("ok");
        var next = Substitute.For<RequestHandlerDelegate<TestResponse>>();
        next().Returns(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await next.Received(1)();
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallNext()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("valid");
        var expectedResponse = new TestResponse("ok");
        var next = Substitute.For<RequestHandlerDelegate<TestResponse>>();
        next().Returns(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Property1", "Error message 1"),
            new("Property2", "Error message 2")
        };
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("invalid");
        var next = Substitute.For<RequestHandlerDelegate<TestResponse>>();

        // Act
        var act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldNotCallNext()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Property1", "Error message")
        };
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("invalid");
        var next = Substitute.For<RequestHandlerDelegate<TestResponse>>();

        // Act
        var act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        await next.DidNotReceive()();
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldAggregateErrors()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<TestRequest>>();
        validator1.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Prop1", "Error1") }));

        var validator2 = Substitute.For<IValidator<TestRequest>>();
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Prop2", "Error2") }));

        var validators = new[] { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("invalid");
        var next = Substitute.For<RequestHandlerDelegate<TestResponse>>();

        // Act
        var act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithOneValidAndOneInvalidValidator_ShouldThrow()
    {
        // Arrange
        var validValidator = Substitute.For<IValidator<TestRequest>>();
        validValidator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var invalidValidator = Substitute.For<IValidator<TestRequest>>();
        invalidValidator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Prop", "Error") }));

        var validators = new[] { validValidator, invalidValidator };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("test");
        var next = Substitute.For<RequestHandlerDelegate<TestResponse>>();

        // Act
        var act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    public record TestRequest(string Value) : IRequest<TestResponse>;
    public record TestResponse(string Result);
}
