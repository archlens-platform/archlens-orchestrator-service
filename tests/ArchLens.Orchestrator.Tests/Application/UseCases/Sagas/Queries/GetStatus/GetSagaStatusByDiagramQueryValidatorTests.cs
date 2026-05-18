using ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.GetStatus;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ArchLens.Orchestrator.Tests.Application.UseCases.Sagas.Queries.GetStatus;

public class GetSagaStatusByDiagramQueryValidatorTests
{
    private readonly GetSagaStatusByDiagramQueryValidator _sut = new();

    [Fact]
    public void Validate_WithValidDiagramId_ShouldNotHaveErrors()
    {
        // Arrange
        var query = new GetSagaStatusByDiagramQuery(Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyDiagramId_ShouldHaveError()
    {
        // Arrange
        var query = new GetSagaStatusByDiagramQuery(Guid.Empty);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiagramId)
            .WithErrorMessage("DiagramId is required.");
    }

    [Fact]
    public void Validate_WithDefaultGuid_ShouldHaveError()
    {
        // Arrange
        var query = new GetSagaStatusByDiagramQuery(default);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiagramId);
    }

    [Fact]
    public void Validate_WithValidGuid_ShouldBeValid()
    {
        // Arrange
        var query = new GetSagaStatusByDiagramQuery(Guid.Parse("12345678-1234-1234-1234-123456789012"));

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
