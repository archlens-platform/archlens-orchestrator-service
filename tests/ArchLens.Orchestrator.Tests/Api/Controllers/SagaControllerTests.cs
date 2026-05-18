using ArchLens.Orchestrator.Api.Controllers;
using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.GetStatus;
using ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.List;
using ArchLens.SharedKernel.Application;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace ArchLens.Orchestrator.Tests.Api.Controllers;

public class SagaControllerTests
{
    private readonly IMediator _mediator;
    private readonly ISagaStateRepository _sagaRepo;
    private readonly SagaController _sut;

    public SagaControllerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _sagaRepo = Substitute.For<ISagaStateRepository>();
        _sut = new SagaController(_mediator, _sagaRepo);
    }

    [Fact]
    public async Task GetByDiagram_WhenFound_ShouldReturnOk()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        var response = CreateSagaStatusResponse(diagramId: diagramId);
        _mediator.Send(Arg.Any<GetSagaStatusByDiagramQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<SagaStatusResponse>(response));

        // Act
        var result = await _sut.GetByDiagram(diagramId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetByDiagram_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetSagaStatusByDiagramQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SagaStatusResponse>(Error.NotFound));

        // Act
        var result = await _sut.GetByDiagram(diagramId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetByAnalysis_WhenFound_ShouldReturnOk()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        var response = CreateSagaStatusResponse(analysisId: analysisId);
        _mediator.Send(Arg.Any<GetSagaStatusByAnalysisQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<SagaStatusResponse>(response));

        // Act
        var result = await _sut.GetByAnalysis(analysisId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetByAnalysis_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetSagaStatusByAnalysisQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SagaStatusResponse>(Error.NotFound));

        // Act
        var result = await _sut.GetByAnalysis(analysisId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task List_ShouldReturnOk()
    {
        // Arrange
        var items = new List<SagaStatusResponse> { CreateSagaStatusResponse() };
        var pagedResponse = new PagedResponse<SagaStatusResponse>(items, 1, 20, 1);
        _mediator.Send(Arg.Any<ListSagasQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(pagedResponse));

        // Act
        var result = await _sut.List(1, 20, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(pagedResponse);
    }

    [Fact]
    public async Task List_WithCustomPagination_ShouldSendCorrectQuery()
    {
        // Arrange
        var items = new List<SagaStatusResponse>();
        var pagedResponse = new PagedResponse<SagaStatusResponse>(items, 3, 10, 0);
        _mediator.Send(Arg.Any<ListSagasQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(pagedResponse));

        // Act
        await _sut.List(3, 10, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<ListSagasQuery>(q => q.Page == 3 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAdminMetrics_ShouldReturnOk()
    {
        // Arrange
        var metrics = new AdminMetricsResponse(
            10, 5, 2, 3, 1500.0,
            new Dictionary<string, int>(),
            new List<RecentAnalysisDto>());
        _sagaRepo.GetAdminMetricsAsync(Arg.Any<CancellationToken>())
            .Returns(metrics);

        // Act
        var result = await _sut.GetAdminMetrics(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(metrics);
    }

    [Fact]
    public async Task DeleteByDiagram_WhenDeleted_ShouldReturnNoContent()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        _sagaRepo.DeleteByDiagramIdAsync(diagramId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.DeleteByDiagram(diagramId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteByDiagram_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        _sagaRepo.DeleteByDiagramIdAsync(diagramId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.DeleteByDiagram(diagramId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetByDiagram_ShouldPassCorrectDiagramIdToMediator()
    {
        // Arrange
        var diagramId = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetSagaStatusByDiagramQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SagaStatusResponse>(Error.NotFound));

        // Act
        await _sut.GetByDiagram(diagramId, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<GetSagaStatusByDiagramQuery>(q => q.DiagramId == diagramId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByAnalysis_ShouldPassCorrectAnalysisIdToMediator()
    {
        // Arrange
        var analysisId = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetSagaStatusByAnalysisQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SagaStatusResponse>(Error.NotFound));

        // Act
        await _sut.GetByAnalysis(analysisId, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<GetSagaStatusByAnalysisQuery>(q => q.AnalysisId == analysisId),
            Arg.Any<CancellationToken>());
    }

    private static SagaStatusResponse CreateSagaStatusResponse(
        Guid? diagramId = null,
        Guid? analysisId = null)
    {
        return new SagaStatusResponse(
            Guid.NewGuid(),
            diagramId ?? Guid.NewGuid(),
            analysisId ?? Guid.NewGuid(),
            "Processing",
            "test.puml",
            0,
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow);
    }
}
