namespace ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;

public record AdminMetricsResponse(
    int TotalAnalyses,
    int Completed,
    int Failed,
    int Processing,
    double AverageProcessingTimeMs,
    Dictionary<string, int> AnalysesByState,
    List<RecentAnalysisDto> RecentAnalyses);

public record RecentAnalysisDto(
    Guid AnalysisId,
    Guid DiagramId,
    string CurrentState,
    string? FileName,
    long? ProcessingTimeMs,
    DateTime CreatedAt);
