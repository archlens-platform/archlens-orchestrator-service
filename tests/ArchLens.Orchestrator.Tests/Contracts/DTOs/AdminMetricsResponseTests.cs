using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using FluentAssertions;

namespace ArchLens.Orchestrator.Tests.Contracts.DTOs;

public class AdminMetricsResponseTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var analysesByState = new Dictionary<string, int>
        {
            { "Processing", 5 },
            { "Completed", 10 },
            { "Failed", 2 }
        };
        var recentAnalyses = new List<RecentAnalysisDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Completed", "file1.puml", 1000, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), "Processing", "file2.puml", null, DateTime.UtcNow)
        };

        // Act
        var response = new AdminMetricsResponse(
            17, 10, 2, 5, 1500.5, analysesByState, recentAnalyses);

        // Assert
        response.TotalAnalyses.Should().Be(17);
        response.Completed.Should().Be(10);
        response.Failed.Should().Be(2);
        response.Processing.Should().Be(5);
        response.AverageProcessingTimeMs.Should().Be(1500.5);
        response.AnalysesByState.Should().HaveCount(3);
        response.RecentAnalyses.Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithEmptyCollections_ShouldSucceed()
    {
        // Act
        var response = new AdminMetricsResponse(
            0, 0, 0, 0, 0.0,
            new Dictionary<string, int>(),
            new List<RecentAnalysisDto>());

        // Assert
        response.TotalAnalyses.Should().Be(0);
        response.AnalysesByState.Should().BeEmpty();
        response.RecentAnalyses.Should().BeEmpty();
    }

    [Fact]
    public void RecentAnalysisDto_ShouldSetAllProperties()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        var diagramId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var dto = new RecentAnalysisDto(analysisId, diagramId, "Completed", "test.puml", 2000, now);

        // Assert
        dto.AnalysisId.Should().Be(analysisId);
        dto.DiagramId.Should().Be(diagramId);
        dto.CurrentState.Should().Be("Completed");
        dto.FileName.Should().Be("test.puml");
        dto.ProcessingTimeMs.Should().Be(2000);
        dto.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void RecentAnalysisDto_WithNullOptionalFields_ShouldSucceed()
    {
        // Act
        var dto = new RecentAnalysisDto(Guid.NewGuid(), Guid.NewGuid(), "Processing", null, null, DateTime.UtcNow);

        // Assert
        dto.FileName.Should().BeNull();
        dto.ProcessingTimeMs.Should().BeNull();
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var analysesByState = new Dictionary<string, int> { { "Processing", 5 } };
        var recentAnalyses = new List<RecentAnalysisDto>();

        var response1 = new AdminMetricsResponse(10, 5, 2, 3, 1000.0, analysesByState, recentAnalyses);
        var response2 = new AdminMetricsResponse(10, 5, 2, 3, 1000.0, analysesByState, recentAnalyses);

        // Assert
        response1.Should().Be(response2);
    }
}
