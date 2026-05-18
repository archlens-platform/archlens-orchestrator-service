using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.GetStatus;
using ArchLens.SharedKernel.Application;
using FluentAssertions;
using NSubstitute;

namespace ArchLens.Orchestrator.Tests.Application.UseCases.Sagas.Queries.GetStatus;

public class GetSagaStatusByAnalysisHandlerTests
{
    private readonly ISagaStateRepository _repository;
    private readonly GetSagaStatusByAnalysisHandler _sut;

    public GetSagaStatusByAnalysisHandlerTests()
    {
        _repository = Substitute.For<ISagaStateRepository>();
        _sut = new GetSagaStatusByAnalysisHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenSagaExists_ShouldReturnSuccess()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        var response = CreateSagaStatusResponse(analysisId: analysisId);
        _repository.GetByAnalysisIdAsync(analysisId, Arg.Any<CancellationToken>())
            .Returns(response);

        var query = new GetSagaStatusByAnalysisQuery(analysisId);

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
        var analysisId = Guid.NewGuid();
        _repository.GetByAnalysisIdAsync(analysisId, Arg.Any<CancellationToken>())
            .Returns((SagaStatusResponse?)null);

        var query = new GetSagaStatusByAnalysisQuery(analysisId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectAnalysisId()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        _repository.GetByAnalysisIdAsync(analysisId, Arg.Any<CancellationToken>())
            .Returns((SagaStatusResponse?)null);

        var query = new GetSagaStatusByAnalysisQuery(analysisId);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByAnalysisIdAsync(analysisId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _repository.GetByAnalysisIdAsync(analysisId, ct)
            .Returns((SagaStatusResponse?)null);

        var query = new GetSagaStatusByAnalysisQuery(analysisId);

        // Act
        await _sut.Handle(query, ct);

        // Assert
        await _repository.Received(1).GetByAnalysisIdAsync(analysisId, ct);
    }

    [Fact]
    public async Task Handle_WhenSagaExists_ShouldReturnMatchingValues()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        var diagramId = Guid.NewGuid();
        var response = CreateSagaStatusResponse(analysisId: analysisId, diagramId: diagramId, currentState: "Completed");
        _repository.GetByAnalysisIdAsync(analysisId, Arg.Any<CancellationToken>())
            .Returns(response);

        var query = new GetSagaStatusByAnalysisQuery(analysisId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Value.AnalysisId.Should().Be(analysisId);
        result.Value.DiagramId.Should().Be(diagramId);
        result.Value.CurrentState.Should().Be("Completed");
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
