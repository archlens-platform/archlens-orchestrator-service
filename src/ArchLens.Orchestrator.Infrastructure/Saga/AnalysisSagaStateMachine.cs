using ArchLens.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ArchLens.Orchestrator.Infrastructure.Saga;

public sealed class AnalysisSagaStateMachine : MassTransitStateMachine<AnalysisSagaState>
{
    private readonly ILogger<AnalysisSagaStateMachine> _logger;

    public State Processing { get; private set; } = null!;
    public State Analyzed { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<DiagramUploadedEvent> DiagramUploaded { get; private set; } = null!;
    public Event<AnalysisCompletedEvent> AnalysisCompleted { get; private set; } = null!;
    public Event<AnalysisFailedEvent> AnalysisFailed { get; private set; } = null!;
    public Event<ReportGeneratedEvent> ReportGenerated { get; private set; } = null!;
    public Event<ReportFailedEvent> ReportFailed { get; private set; } = null!;

    private const int MaxRetries = 3;

    public AnalysisSagaStateMachine(ILogger<AnalysisSagaStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        ConfigureEvents();
        ConfigureInitialState();
        ConfigureProcessingState();
        ConfigureAnalyzedState();

    }

    private void ConfigureEvents()
    {
        Event(() => DiagramUploaded, e => e.CorrelateById(m => m.Message.DiagramId));
        Event(() => AnalysisCompleted, e => e.CorrelateById(m => m.Message.DiagramId));
        Event(() => AnalysisFailed, e => e.CorrelateById(m => m.Message.DiagramId));
        Event(() => ReportGenerated, e => e.CorrelateById(m => m.Message.DiagramId));
        Event(() => ReportFailed, e => e.CorrelateById(m => m.Message.DiagramId));
    }

    private void ConfigureInitialState()
    {
        Initially(
            When(DiagramUploaded)
                .Then(context =>
                {
                    var analysisId = Guid.NewGuid();
                    context.Saga.AnalysisId = analysisId;
                    context.Saga.DiagramId = context.Message.DiagramId;
                    context.Saga.FileName = context.Message.FileName;
                    context.Saga.FileHash = context.Message.FileHash;
                    context.Saga.StoragePath = context.Message.StoragePath;
                    context.Saga.UserId = context.Message.UserId;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Saga created for diagram {DiagramId}, analysis {AnalysisId}",
                        context.Message.DiagramId, analysisId);
                })
                .PublishAsync(context => context.Init<ProcessingStartedEvent>(new
                {
                    context.Saga.AnalysisId,
                    context.Saga.DiagramId,
                    StoragePath = context.Saga.StoragePath ?? string.Empty,
                    Timestamp = DateTime.UtcNow
                }))
                .PublishAsync(context => context.Init<StatusChangedEvent>(new
                {
                    context.Saga.AnalysisId,
                    OldStatus = "Received",
                    NewStatus = "Processing",
                    Timestamp = DateTime.UtcNow
                }))
                .TransitionTo(Processing));
    }

    private void ConfigureProcessingState()
    {
        During(Processing,
            When(AnalysisCompleted)
                .Then(context =>
                {
                    context.Saga.ResultJson = context.Message.ResultJson;
                    context.Saga.ProcessingTimeMs = context.Message.ProcessingTimeMs;
                    context.Saga.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Analysis completed for {DiagramId} in {Ms}ms using {Providers}",
                        context.Saga.DiagramId,
                        context.Message.ProcessingTimeMs,
                        string.Join(", ", context.Message.ProvidersUsed));
                })
                .PublishAsync(context => context.Init<GenerateReportCommand>(new
                {
                    context.Saga.AnalysisId,
                    context.Saga.DiagramId,
                    UserId = context.Saga.UserId,
                    context.Message.ResultJson,
                    context.Message.ProvidersUsed,
                    context.Message.ProcessingTimeMs,
                    Timestamp = DateTime.UtcNow
                }))
                .PublishAsync(context => context.Init<StatusChangedEvent>(new
                {
                    context.Saga.AnalysisId,
                    OldStatus = "Processing",
                    NewStatus = "Analyzed",
                    Timestamp = DateTime.UtcNow
                }))
                .TransitionTo(Analyzed),

            When(AnalysisFailed)
                .IfElse(
                    context => context.Saga.RetryCount < MaxRetries,
                    retry => retry
                        .Then(context =>
                        {
                            context.Saga.RetryCount++;
                            context.Saga.UpdatedAt = DateTime.UtcNow;

                            _logger.LogWarning(
                                "Analysis failed for {DiagramId}, retry {Retry}/{Max}: {Error}",
                                context.Saga.DiagramId,
                                context.Saga.RetryCount,
                                MaxRetries,
                                context.Message.ErrorMessage);
                        })
                        .PublishAsync(context => context.Init<ProcessingStartedEvent>(new
                        {
                            context.Saga.AnalysisId,
                            context.Saga.DiagramId,
                            StoragePath = context.Saga.StoragePath ?? string.Empty,
                            Timestamp = DateTime.UtcNow
                        })),
                    fail => fail
                        .Then(context =>
                        {
                            context.Saga.ErrorMessage = context.Message.ErrorMessage;
                            context.Saga.UpdatedAt = DateTime.UtcNow;

                            _logger.LogError(
                                "Analysis permanently failed for {DiagramId} after {Max} retries: {Error}",
                                context.Saga.DiagramId,
                                MaxRetries,
                                context.Message.ErrorMessage);
                        })
                        .PublishAsync(context => context.Init<StatusChangedEvent>(new
                        {
                            context.Saga.AnalysisId,
                            OldStatus = "Processing",
                            NewStatus = "Failed",
                            Timestamp = DateTime.UtcNow
                        }))
                        .TransitionTo(Failed)));
    }

    private void ConfigureAnalyzedState()
    {
        During(Analyzed,
            When(ReportGenerated)
                .Then(context =>
                {
                    context.Saga.ReportId = context.Message.ReportId;
                    context.Saga.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Report {ReportId} generated for diagram {DiagramId}",
                        context.Message.ReportId,
                        context.Saga.DiagramId);
                })
                .PublishAsync(context => context.Init<StatusChangedEvent>(new
                {
                    context.Saga.AnalysisId,
                    OldStatus = "Analyzed",
                    NewStatus = "Completed",
                    Timestamp = DateTime.UtcNow
                }))
                .TransitionTo(Completed),

            When(ReportFailed)
                .Then(context =>
                {
                    context.Saga.ErrorMessage = context.Message.ErrorMessage;
                    context.Saga.UpdatedAt = DateTime.UtcNow;

                    _logger.LogError(
                        "Report generation failed for {DiagramId}: {Error}",
                        context.Saga.DiagramId,
                        context.Message.ErrorMessage);
                })
                .PublishAsync(context => context.Init<StatusChangedEvent>(new
                {
                    context.Saga.AnalysisId,
                    OldStatus = "Analyzed",
                    NewStatus = "Failed",
                    Timestamp = DateTime.UtcNow
                }))
                .TransitionTo(Failed));
    }
}
