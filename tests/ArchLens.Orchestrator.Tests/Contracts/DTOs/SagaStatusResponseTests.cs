using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using FluentAssertions;

namespace ArchLens.Orchestrator.Tests.Contracts.DTOs;

public class SagaStatusResponseTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var diagramId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var response = new SagaStatusResponse(
            correlationId, diagramId, analysisId,
            "Completed", "test.puml", 1,
            "error msg", reportId, 2500,
            now, now);

        // Assert
        response.CorrelationId.Should().Be(correlationId);
        response.DiagramId.Should().Be(diagramId);
        response.AnalysisId.Should().Be(analysisId);
        response.CurrentState.Should().Be("Completed");
        response.FileName.Should().Be("test.puml");
        response.RetryCount.Should().Be(1);
        response.ErrorMessage.Should().Be("error msg");
        response.ReportId.Should().Be(reportId);
        response.ProcessingTimeMs.Should().Be(2500);
        response.CreatedAt.Should().Be(now);
        response.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Create_WithNullableFields_ShouldAcceptNull()
    {
        // Act
        var response = new SagaStatusResponse(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Processing", null, 0,
            null, null, null,
            DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        response.FileName.Should().BeNull();
        response.ErrorMessage.Should().BeNull();
        response.ReportId.Should().BeNull();
        response.ProcessingTimeMs.Should().BeNull();
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var diagramId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var response1 = new SagaStatusResponse(
            correlationId, diagramId, analysisId,
            "Processing", "file.puml", 0,
            null, null, null,
            now, now);

        var response2 = new SagaStatusResponse(
            correlationId, diagramId, analysisId,
            "Processing", "file.puml", 0,
            null, null, null,
            now, now);

        // Assert
        response1.Should().Be(response2);
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var response1 = new SagaStatusResponse(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Processing", "file.puml", 0,
            null, null, null,
            now, now);

        var response2 = new SagaStatusResponse(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Completed", "other.puml", 1,
            null, null, null,
            now, now);

        // Assert
        response1.Should().NotBe(response2);
    }
}
