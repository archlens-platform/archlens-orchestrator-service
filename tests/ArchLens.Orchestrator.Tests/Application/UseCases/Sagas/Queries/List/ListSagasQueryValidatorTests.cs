using ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.List;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ArchLens.Orchestrator.Tests.Application.UseCases.Sagas.Queries.List;

public class ListSagasQueryValidatorTests
{
    private readonly ListSagasQueryValidator _sut = new();

    [Fact]
    public void Validate_WithDefaultValues_ShouldBeValid()
    {
        // Arrange
        var query = new ListSagasQuery();

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 50)]
    [InlineData(1, 100)]
    [InlineData(5, 20)]
    [InlineData(100, 10)]
    public void Validate_WithValidPageAndPageSize_ShouldBeValid(int page, int pageSize)
    {
        // Arrange
        var query = new ListSagasQuery(page, pageSize);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithInvalidPage_ShouldHaveError(int page)
    {
        // Arrange
        var query = new ListSagasQuery(page, 20);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Page)
            .WithErrorMessage("Page must be greater than or equal to 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(200)]
    public void Validate_WithInvalidPageSize_ShouldHaveError(int pageSize)
    {
        // Arrange
        var query = new ListSagasQuery(1, pageSize);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorMessage("PageSize must be between 1 and 100.");
    }

    [Fact]
    public void Validate_WithPageSizeOfOne_ShouldBeValid()
    {
        // Arrange
        var query = new ListSagasQuery(1, 1);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithPageSizeOfHundred_ShouldBeValid()
    {
        // Arrange
        var query = new ListSagasQuery(1, 100);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithBothInvalid_ShouldHaveMultipleErrors()
    {
        // Arrange
        var query = new ListSagasQuery(0, 0);

        // Act
        var result = _sut.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Page);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
        result.Errors.Should().HaveCount(2);
    }
}
