using System.Security.Claims;
using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.GetStatus;
using ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.List;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLens.Orchestrator.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public sealed class SagaController(IMediator mediator, ISagaStateRepository sagaRepo) : ControllerBase
{
    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    [HttpGet("diagram/{diagramId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByDiagram(Guid diagramId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSagaStatusByDiagramQuery(diagramId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpGet("analysis/{analysisId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAnalysis(Guid analysisId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSagaStatusByAnalysisQuery(analysisId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new ListSagasQuery(page, pageSize, GetCurrentUserId(), false), ct);
        return Ok(result.Value);
    }

    [HttpGet("admin/metrics")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminMetrics(CancellationToken ct)
    {
        var metrics = await sagaRepo.GetAdminMetricsAsync(ct);
        return Ok(metrics);
    }

    [HttpDelete("diagram/{diagramId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteByDiagram(Guid diagramId, CancellationToken ct)
    {
        var deleted = await sagaRepo.DeleteByDiagramIdAsync(diagramId, ct);
        return deleted ? NoContent() : NotFound();
    }
}
