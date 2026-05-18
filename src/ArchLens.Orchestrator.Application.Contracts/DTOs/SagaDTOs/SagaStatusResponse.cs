namespace ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;

public record SagaStatusResponse(
    Guid CorrelationId,
    Guid DiagramId,
    Guid AnalysisId,
    string CurrentState,
    string? FileName,
    int RetryCount,
    string? ErrorMessage,
    Guid? ReportId,
    long? ProcessingTimeMs,
    DateTime CreatedAt,
    DateTime UpdatedAt);
