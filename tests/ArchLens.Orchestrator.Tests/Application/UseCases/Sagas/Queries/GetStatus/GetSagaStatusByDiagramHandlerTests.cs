using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.GetStatus;
using ArchLens.SharedKernel.Application;
using FluentAssertions;
using NSubstitute;

namespace ArchLens.Orchestrator.Tests.Application.UseCases.Sagas.Queries.GetStatus;

public class GetSagaStatusByDiagramHandlerTests
{
    private readonly ISagaStateRepository _repository;
    private readonly GetSagaStatusByDiagramHandler _sut;

    public GetSagaStatusByDiagramHandlerTests()
    {
        _repository = Substitute.For<ISagaStateRepository>();
        _sut = new GetSagaStatusByDiagramHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenSagaExists_ShouldReturnSuccess()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        var response = CreateSagaStatusResponse(diagramId: diagramId);
        _repository.GetByDiagramIdAsync(diagramId, Arg.Any<CancellationToken>())
            .Returns(response);

        var query = new GetSagaStatusByDiagramQuery(diagramId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(response);
    }

    [Fact]
    public async Task Handle_WhenSagaDoesNotExist_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        _repository.GetByDiagramIdAsync(diagramId, Arg.Any<CancellationToken>())
            .Returns((SagaStatusResponse?)null);

        var query = new GetSagaStatusByDiagramQuery(diagramId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectDiagramId()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        _repository.GetByDiagramIdAsync(diagramId, Arg.Any<CancellationToken>())
            .Returns((SagaStatusResponse?)null);

        var query = new GetSagaStatusByDiagramQuery(diagramId);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByDiagramIdAsync(diagramId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _repository.GetByDiagramIdAsync(diagramId, ct)
            .Returns((SagaStatusResponse?)null);

        var query = new GetSagaStatusByDiagramQuery(diagramId);

        // Act
        await _sut.Handle(query, ct);

        // Assert
        await _repository.Received(1).GetByDiagramIdAsync(diagramId, ct);
    }

    [Fact]
    public async Task Handle_WhenSagaExists_ShouldReturnMatchingValues()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var response = CreateSagaStatusResponse(
            diagramId: diagramId,
            analysisId: analysisId,
            currentState: "Completed",
            reportId: reportId,
            processingTimeMs: 1500);
        _repository.GetByDiagramIdAsync(diagramId, Arg.Any<CancellationToken>())
            .Returns(response);

        var query = new GetSagaStatusByDiagramQuery(diagramId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Value.DiagramId.Should().Be(diagramId);
        result.Value.AnalysisId.Should().Be(analysisId);
        result.Value.CurrentState.Should().Be("Completed");
        result.Value.ReportId.Should().Be(reportId);
        result.Value.ProcessingTimeMs.Should().Be(1500);
    }

    private static SagaStatusResponse CreateSagaStatusResponse(
        Guid? correlationId = null,
        Guid? diagramId = null,
        Guid? analysisId = null,
        string currentState = "Processing",
        string? fileName = "test.puml",
        int retryCount = 0,
        string? errorMessage = null,
        Guid? reportId = null,
        long? processingTimeMs = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        return new SagaStatusResponse(
            correlationId ?? Guid.NewGuid(),
            diagramId ?? Guid.NewGuid(),
            analysisId ?? Guid.NewGuid(),
            currentState,
            fileName,
            retryCount,
            errorMessage,
            reportId,
            processingTimeMs,
            createdAt ?? DateTime.UtcNow,
            updatedAt ?? DateTime.UtcNow);
    }
}
