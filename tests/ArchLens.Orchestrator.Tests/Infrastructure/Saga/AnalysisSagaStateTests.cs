using ArchLens.Orchestrator.Infrastructure.Saga;
using FluentAssertions;

namespace ArchLens.Orchestrator.Tests.Infrastructure.Saga;

public class AnalysisSagaStateTests
{
    [Fact]
    public void Constructor_ShouldCreateWithDefaultValues()
    {
        // Act
        var state = new AnalysisSagaState();

        // Assert
        state.CorrelationId.Should().Be(Guid.Empty);
        state.CurrentState.Should().BeNull();
        state.AnalysisId.Should().Be(Guid.Empty);
        state.DiagramId.Should().Be(Guid.Empty);
        state.FileName.Should().BeNull();
        state.FileHash.Should().BeNull();
        state.StoragePath.Should().BeNull();
        state.UserId.Should().BeNull();
        state.RetryCount.Should().Be(0);
        state.ErrorMessage.Should().BeNull();
        state.ResultJson.Should().BeNull();
        state.ReportId.Should().BeNull();
        state.ProcessingTimeMs.Should().BeNull();
        state.RowVersion.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();
        var diagramId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var state = new AnalysisSagaState
        {
            CorrelationId = correlationId,
            CurrentState = "Processing",
            AnalysisId = analysisId,
            DiagramId = diagramId,
            FileName = "diagram.puml",
            FileHash = "abc123",
            StoragePath = "/storage/diagram.puml",
            UserId = "user-1",
            RetryCount = 2,
            ErrorMessage = "Some error",
            ResultJson = "{\"result\": true}",
            ReportId = reportId,
            ProcessingTimeMs = 1500,
            RowVersion = new byte[] { 1, 2, 3 },
            CreatedAt = now,
            UpdatedAt = now
        };

        // Assert
        state.CorrelationId.Should().Be(correlationId);
        state.CurrentState.Should().Be("Processing");
        state.AnalysisId.Should().Be(analysisId);
        state.DiagramId.Should().Be(diagramId);
        state.FileName.Should().Be("diagram.puml");
        state.FileHash.Should().Be("abc123");
        state.StoragePath.Should().Be("/storage/diagram.puml");
        state.UserId.Should().Be("user-1");
        state.RetryCount.Should().Be(2);
        state.ErrorMessage.Should().Be("Some error");
        state.ResultJson.Should().Be("{\"result\": true}");
        state.ReportId.Should().Be(reportId);
        state.ProcessingTimeMs.Should().Be(1500);
        state.RowVersion.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        state.CreatedAt.Should().Be(now);
        state.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void RetryCount_ShouldBeIncrementable()
    {
        // Arrange
        var state = new AnalysisSagaState { RetryCount = 0 };

        // Act
        state.RetryCount++;
        state.RetryCount++;
        state.RetryCount++;

        // Assert
        state.RetryCount.Should().Be(3);
    }

    [Fact]
    public void ProcessingTimeMs_ShouldAcceptNullAndValue()
    {
        // Arrange
        var state = new AnalysisSagaState();

        // Assert - default null
        state.ProcessingTimeMs.Should().BeNull();

        // Act - set value
        state.ProcessingTimeMs = 5000;

        // Assert
        state.ProcessingTimeMs.Should().Be(5000);
    }

    [Fact]
    public void ReportId_ShouldAcceptNullAndValue()
    {
        // Arrange
        var state = new AnalysisSagaState();
        var reportId = Guid.NewGuid();

        // Assert - default null
        state.ReportId.Should().BeNull();

        // Act
        state.ReportId = reportId;

        // Assert
        state.ReportId.Should().Be(reportId);
    }

    [Fact]
    public void UpdatedAt_ShouldBeUpdatable()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        var updatedAt = DateTime.UtcNow;
        var state = new AnalysisSagaState
        {
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Act
        state.UpdatedAt = updatedAt;

        // Assert
        state.CreatedAt.Should().Be(createdAt);
        state.UpdatedAt.Should().Be(updatedAt);
        state.UpdatedAt.Should().BeAfter(state.CreatedAt);
    }
}
