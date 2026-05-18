using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Context;
using ArchLens.Orchestrator.Infrastructure.Saga;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Repositories.SagaStateRepositories;

public sealed class SagaStateRepository(SagaDbContext dbContext) : ISagaStateRepository
{
    public async Task<SagaStatusResponse?> GetByDiagramIdAsync(Guid diagramId, CancellationToken ct = default)
    {
        var state = await dbContext.SagaStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DiagramId == diagramId, ct);

        return state is null ? null : MapToResponse(state);
    }

    public async Task<SagaStatusResponse?> GetByAnalysisIdAsync(Guid analysisId, CancellationToken ct = default)
    {
        var state = await dbContext.SagaStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AnalysisId == analysisId, ct);

        return state is null ? null : MapToResponse(state);
    }

    public async Task<IReadOnlyList<SagaStatusResponse>> ListAsync(int page, int pageSize, string? userId = null, CancellationToken ct = default)
    {
        var query = dbContext.SagaStates.AsNoTracking().AsQueryable();

        if (userId is not null)
            query = query.Where(x => x.UserId == userId);

        var states = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return states.Select(MapToResponse).ToList();
    }

    public async Task<bool> DeleteByDiagramIdAsync(Guid diagramId, CancellationToken ct = default)
    {
        var state = await dbContext.SagaStates
            .FirstOrDefaultAsync(x => x.DiagramId == diagramId, ct);

        if (state is null) return false;

        dbContext.SagaStates.Remove(state);
        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AdminMetricsResponse> GetAdminMetricsAsync(CancellationToken ct = default)
    {
        var allStates = await dbContext.SagaStates
            .AsNoTracking()
            .ToListAsync(ct);

        var total = allStates.Count;
        var completed = allStates.Count(x => x.CurrentState == "Completed");
        var failed = allStates.Count(x => x.CurrentState == "Failed");
        var processing = allStates.Count(x => x.CurrentState != "Completed" && x.CurrentState != "Failed");

        var avgProcessingTime = allStates
            .Where(x => x.CurrentState == "Completed" && x.ProcessingTimeMs.HasValue)
            .Select(x => (double)x.ProcessingTimeMs!.Value)
            .DefaultIfEmpty(0)
            .Average();

        var analysesByState = allStates
            .GroupBy(x => x.CurrentState)
            .ToDictionary(g => g.Key, g => g.Count());

        var recentAnalyses = allStates
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .Select(x => new RecentAnalysisDto(
                x.AnalysisId,
                x.DiagramId,
                x.CurrentState,
                x.FileName,
                x.ProcessingTimeMs,
                x.CreatedAt))
            .ToList();

        return new AdminMetricsResponse(
            total,
            completed,
            failed,
            processing,
            avgProcessingTime,
            analysesByState,
            recentAnalyses);
    }

    private static SagaStatusResponse MapToResponse(AnalysisSagaState state) => new(
        state.CorrelationId,
        state.DiagramId,
        state.AnalysisId,
        state.CurrentState,
        state.FileName,
        state.RetryCount,
        state.ErrorMessage,
        state.ReportId,
        state.ProcessingTimeMs,
        state.CreatedAt,
        state.UpdatedAt);
}
