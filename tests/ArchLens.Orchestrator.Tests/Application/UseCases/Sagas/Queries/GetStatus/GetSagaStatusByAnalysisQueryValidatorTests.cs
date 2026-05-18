using ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.GetStatus;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ArchLens.Orchestrator.Tests.Application.UseCases.Sagas.Queries.GetStatus;

public class GetSagaStatusByAnalysisQueryValidatorTests
{
    private readonly GetSagaStatusByAnalysisQueryValidator _sut = new();

    [Fact]
    public void Validate_WithValidAnalysisId_ShouldNotHaveErrors()
    {
        // Arrange
        var query = new GetSagaStatusByAnalysisQuery(Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyAnalysisId_ShouldHaveError()
    {
        // Arrange
        var query = new GetSagaStatusByAnalysisQuery(Guid.Empty);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisId)
            .WithErrorMessage("AnalysisId is required.");
    }

    [Fact]
    public void Validate_WithDefaultGuid_ShouldHaveError()
    {
        // Arrange
        var query = new GetSagaStatusByAnalysisQuery(default);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AnalysisId);
    }

    [Fact]
    public void Validate_WithValidGuid_ShouldBeValid()
    {
        // Arrange
        var query = new GetSagaStatusByAnalysisQuery(Guid.Parse("12345678-1234-1234-1234-123456789012"));

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
