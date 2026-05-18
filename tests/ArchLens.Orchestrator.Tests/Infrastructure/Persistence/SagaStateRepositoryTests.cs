using ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Context;
using ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Repositories.SagaStateRepositories;
using ArchLens.Orchestrator.Infrastructure.Saga;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Orchestrator.Tests.Infrastructure.Persistence;

public class SagaStateRepositoryTests : IDisposable
{
    private readonly SagaDbContext _context;
    private readonly SagaStateRepository _sut;

    public SagaStateRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SagaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SagaDbContext(options);
        _sut = new SagaStateRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByDiagramIdAsync

    [Fact]
    public async Task GetByDiagramIdAsync_WhenStateExists_ShouldReturnMappedResponse()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        var state = CreateSagaState(diagramId: diagramId, currentState: "Processing");
        _context.SagaStates.Add(state);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByDiagramIdAsync(diagramId);

        // Assert
        result.Should().NotBeNull();
        result!.DiagramId.Should().Be(diagramId);
        result.CurrentState.Should().Be("Processing");
        result.CorrelationId.Should().Be(state.CorrelationId);
        result.AnalysisId.Should().Be(state.AnalysisId);
        result.FileName.Should().Be(state.FileName);
        result.RetryCount.Should().Be(state.RetryCount);
        result.ErrorMessage.Should().Be(state.ErrorMessage);
        result.ReportId.Should().Be(state.ReportId);
        result.ProcessingTimeMs.Should().Be(state.ProcessingTimeMs);
    }

    [Fact]
    public async Task GetByDiagramIdAsync_WhenStateDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetByDiagramIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByDiagramIdAsync_WithEmptyGuid_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetByDiagramIdAsync(Guid.Empty);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByAnalysisIdAsync

    [Fact]
    public async Task GetByAnalysisIdAsync_WhenStateExists_ShouldReturnMappedResponse()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        var state = CreateSagaState(analysisId: analysisId, currentState: "Completed");
        _context.SagaStates.Add(state);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByAnalysisIdAsync(analysisId);

        // Assert
        result.Should().NotBeNull();
        result!.AnalysisId.Should().Be(analysisId);
        result.CurrentState.Should().Be("Completed");
    }

    [Fact]
    public async Task GetByAnalysisIdAsync_WhenStateDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetByAnalysisIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByAnalysisIdAsync_WithMultipleStates_ShouldReturnCorrectOne()
    {
        // Arrange
        var targetAnalysisId = Guid.NewGuid();
        _context.SagaStates.AddRange(
            CreateSagaState(analysisId: Guid.NewGuid(), currentState: "Processing"),
            CreateSagaState(analysisId: targetAnalysisId, currentState: "Completed"),
            CreateSagaState(analysisId: Guid.NewGuid(), currentState: "Failed"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByAnalysisIdAsync(targetAnalysisId);

        // Assert
        result.Should().NotBeNull();
        result!.AnalysisId.Should().Be(targetAnalysisId);
        result.CurrentState.Should().Be("Completed");
    }

    #endregion

    #region ListAsync

    [Fact]
    public async Task ListAsync_WithData_ShouldReturnPagedResults()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _context.SagaStates.Add(CreateSagaState(
                createdAt: DateTime.UtcNow.AddMinutes(-i)));
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ListAsync(1, 3);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListAsync_SecondPage_ShouldSkipFirstPageItems()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _context.SagaStates.Add(CreateSagaState(
                createdAt: DateTime.UtcNow.AddMinutes(-i)));
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ListAsync(2, 3);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.ListAsync(1, 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_ShouldOrderByCreatedAtDescending()
    {
        // Arrange
        var oldest = CreateSagaState(createdAt: DateTime.UtcNow.AddHours(-3));
        var middle = CreateSagaState(createdAt: DateTime.UtcNow.AddHours(-2));
        var newest = CreateSagaState(createdAt: DateTime.UtcNow.AddHours(-1));
        _context.SagaStates.AddRange(oldest, middle, newest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ListAsync(1, 10);

        // Assert
        result.Should().HaveCount(3);
        result[0].CreatedAt.Should().BeAfter(result[1].CreatedAt);
        result[1].CreatedAt.Should().BeAfter(result[2].CreatedAt);
    }

    [Fact]
    public async Task ListAsync_PageBeyondData_ShouldReturnEmptyList()
    {
        // Arrange
        _context.SagaStates.Add(CreateSagaState());
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ListAsync(5, 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_PageSizeOfOne_ShouldReturnSingleItem()
    {
        // Arrange
        _context.SagaStates.AddRange(
            CreateSagaState(createdAt: DateTime.UtcNow.AddMinutes(-1)),
            CreateSagaState(createdAt: DateTime.UtcNow));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ListAsync(1, 1);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region DeleteByDiagramIdAsync

    [Fact]
    public async Task DeleteByDiagramIdAsync_WhenStateExists_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        _context.SagaStates.Add(CreateSagaState(diagramId: diagramId));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteByDiagramIdAsync(diagramId);

        // Assert
        result.Should().BeTrue();
        var remaining = await _context.SagaStates.CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task DeleteByDiagramIdAsync_WhenStateDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var result = await _sut.DeleteByDiagramIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteByDiagramIdAsync_ShouldOnlyDeleteMatchingState()
    {
        // Arrange
        var targetDiagramId = Guid.NewGuid();
        _context.SagaStates.AddRange(
            CreateSagaState(diagramId: targetDiagramId),
            CreateSagaState(diagramId: Guid.NewGuid()),
            CreateSagaState(diagramId: Guid.NewGuid()));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteByDiagramIdAsync(targetDiagramId);

        // Assert
        result.Should().BeTrue();
        var remaining = await _context.SagaStates.CountAsync();
        remaining.Should().Be(2);
    }

    #endregion

    #region GetAdminMetricsAsync

    [Fact]
    public async Task GetAdminMetricsAsync_WhenEmpty_ShouldReturnZeroMetrics()
    {
        // Act
        var result = await _sut.GetAdminMetricsAsync();

        // Assert
        result.TotalAnalyses.Should().Be(0);
        result.Completed.Should().Be(0);
        result.Failed.Should().Be(0);
        result.Processing.Should().Be(0);
        result.AverageProcessingTimeMs.Should().Be(0);
        result.AnalysesByState.Should().BeEmpty();
        result.RecentAnalyses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAdminMetricsAsync_ShouldCountStatesCorrectly()
    {
        // Arrange
        _context.SagaStates.AddRange(
            CreateSagaState(currentState: "Completed", processingTimeMs: 1000),
            CreateSagaState(currentState: "Completed", processingTimeMs: 2000),
            CreateSagaState(currentState: "Failed"),
            CreateSagaState(currentState: "Processing"),
            CreateSagaState(currentState: "WaitingForAnalysis"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAdminMetricsAsync();

        // Assert
        result.TotalAnalyses.Should().Be(5);
        result.Completed.Should().Be(2);
        result.Failed.Should().Be(1);
        result.Processing.Should().Be(2);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_ShouldCalculateAverageProcessingTime()
    {
        // Arrange
        _context.SagaStates.AddRange(
            CreateSagaState(currentState: "Completed", processingTimeMs: 1000),
            CreateSagaState(currentState: "Completed", processingTimeMs: 3000),
            CreateSagaState(currentState: "Completed", processingTimeMs: null),
            CreateSagaState(currentState: "Failed", processingTimeMs: 500));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAdminMetricsAsync();

        // Assert
        result.AverageProcessingTimeMs.Should().Be(2000);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_ShouldGroupByState()
    {
        // Arrange
        _context.SagaStates.AddRange(
            CreateSagaState(currentState: "Completed"),
            CreateSagaState(currentState: "Completed"),
            CreateSagaState(currentState: "Failed"),
            CreateSagaState(currentState: "Processing"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAdminMetricsAsync();

        // Assert
        result.AnalysesByState.Should().ContainKey("Completed").WhoseValue.Should().Be(2);
        result.AnalysesByState.Should().ContainKey("Failed").WhoseValue.Should().Be(1);
        result.AnalysesByState.Should().ContainKey("Processing").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_RecentAnalyses_ShouldLimitTo20()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            _context.SagaStates.Add(CreateSagaState(
                createdAt: DateTime.UtcNow.AddMinutes(-i)));
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAdminMetricsAsync();

        // Assert
        result.RecentAnalyses.Should().HaveCount(20);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_RecentAnalyses_ShouldBeOrderedByCreatedAtDescending()
    {
        // Arrange
        _context.SagaStates.AddRange(
            CreateSagaState(createdAt: DateTime.UtcNow.AddHours(-3)),
            CreateSagaState(createdAt: DateTime.UtcNow.AddHours(-1)),
            CreateSagaState(createdAt: DateTime.UtcNow.AddHours(-2)));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAdminMetricsAsync();

        // Assert
        result.RecentAnalyses.Should().HaveCount(3);
        result.RecentAnalyses[0].CreatedAt.Should().BeAfter(result.RecentAnalyses[1].CreatedAt);
        result.RecentAnalyses[1].CreatedAt.Should().BeAfter(result.RecentAnalyses[2].CreatedAt);
    }

    [Fact]
    public async Task GetAdminMetricsAsync_WithNoCompletedStates_ShouldReturnZeroAverage()
    {
        // Arrange
        _context.SagaStates.AddRange(
            CreateSagaState(currentState: "Failed"),
            CreateSagaState(currentState: "Processing"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAdminMetricsAsync();

        // Assert
        result.AverageProcessingTimeMs.Should().Be(0);
    }

    #endregion

    #region Helpers

    private static AnalysisSagaState CreateSagaState(
        Guid? diagramId = null,
        Guid? analysisId = null,
        string currentState = "Processing",
        string? fileName = "test-diagram.puml",
        long? processingTimeMs = null,
        DateTime? createdAt = null)
    {
        var now = createdAt ?? DateTime.UtcNow;
        return new AnalysisSagaState
        {
            CorrelationId = Guid.NewGuid(),
            CurrentState = currentState,
            AnalysisId = analysisId ?? Guid.NewGuid(),
            DiagramId = diagramId ?? Guid.NewGuid(),
            FileName = fileName,
            FileHash = "hash123",
            StoragePath = "/storage/test.puml",
            UserId = "user-1",
            RetryCount = 0,
            ProcessingTimeMs = processingTimeMs,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    #endregion
}
