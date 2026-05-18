using MassTransit;

namespace ArchLens.Orchestrator.Infrastructure.Saga;

public class AnalysisSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;

    public Guid AnalysisId { get; set; }
    public Guid DiagramId { get; set; }
    public string? FileName { get; set; }
    public string? FileHash { get; set; }
    public string? StoragePath { get; set; }
    public string? UserId { get; set; }

    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResultJson { get; set; }
    public Guid? ReportId { get; set; }
    public long? ProcessingTimeMs { get; set; }

    public byte[]? RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
