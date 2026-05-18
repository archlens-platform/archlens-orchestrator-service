using ArchLens.Contracts.Events;
using ArchLens.Orchestrator.Infrastructure.Saga;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLens.Orchestrator.Tests.Infrastructure.Saga;

public class AnalysisSagaStateMachineTransitionTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<AnalysisSagaStateMachine, AnalysisSagaState> _sagaHarness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddLogging()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<AnalysisSagaStateMachine, AnalysisSagaState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();

        _sagaHarness = _harness
            .GetSagaStateMachineHarness<AnalysisSagaStateMachine, AnalysisSagaState>();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task DiagramUploaded_ShouldTransitionToProcessing()
    {
        var diagramId = Guid.NewGuid();

        await _harness.Bus.Publish(new DiagramUploadedEvent
        {
            DiagramId = diagramId,
            FileName = "test.puml",
            FileHash = "abc123",
            StoragePath = "/storage/test.puml",
            UserId = "user-1",
            Timestamp = DateTime.UtcNow
        });

        var instanceId = await _sagaHarness.Exists(diagramId, x => x.Processing);
        instanceId.Should().NotBeNull();
    }

    [Fact]
    public async Task DiagramUploaded_ShouldPublishProcessingStartedEvent()
    {
        var diagramId = Guid.NewGuid();

        await _harness.Bus.Publish(new DiagramUploadedEvent
        {
            DiagramId = diagramId,
            FileName = "test.puml",
            FileHash = "abc123",
            StoragePath = "/storage/test.puml",
            UserId = "user-1",
            Timestamp = DateTime.UtcNow
        });

        (await _harness.Published.Any<ProcessingStartedEvent>()).Should().BeTrue();
        (await _harness.Published.Any<StatusChangedEvent>()).Should().BeTrue();
    }

    [Fact]
    public async Task DiagramUploaded_ShouldPopulateSagaProperties()
    {
        var diagramId = Guid.NewGuid();

        await _harness.Bus.Publish(new DiagramUploadedEvent
        {
            DiagramId = diagramId,
            FileName = "diagram.puml",
            FileHash = "hash-xyz",
            StoragePath = "/blobs/diagram.puml",
            UserId = "user-42",
            Timestamp = DateTime.UtcNow
        });

        var instanceId = await _sagaHarness.Exists(diagramId, x => x.Processing);
        instanceId.Should().NotBeNull();

        var instance = _sagaHarness.Sagas.ContainsInState(diagramId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.Processing);
        instance.Should().NotBeNull();
        instance!.DiagramId.Should().Be(diagramId);
        instance.FileName.Should().Be("diagram.puml");
        instance.FileHash.Should().Be("hash-xyz");
        instance.StoragePath.Should().Be("/blobs/diagram.puml");
        instance.UserId.Should().Be("user-42");
        instance.AnalysisId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalysisCompleted_ShouldTransitionToAnalyzed()
    {
        var diagramId = Guid.NewGuid();

        await _harness.Bus.Publish(new DiagramUploadedEvent
        {
            DiagramId = diagramId,
            FileName = "test.puml",
            FileHash = "abc",
            StoragePath = "/s",
            Timestamp = DateTime.UtcNow
        });
        await _sagaHarness.Exists(diagramId, x => x.Processing);

        await _harness.Bus.Publish(new AnalysisCompletedEvent
        {
            DiagramId = diagramId,
            AnalysisId = Guid.NewGuid(),
            ResultJson = "{\"patterns\":[]}",
            ProvidersUsed = ["OpenAI"],
            ProcessingTimeMs = 1200,
            Timestamp = DateTime.UtcNow
        });

        var instanceId = await _sagaHarness.Exists(diagramId, x => x.Analyzed);
        instanceId.Should().NotBeNull();

        var instance = _sagaHarness.Sagas.ContainsInState(diagramId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.Analyzed);
        instance!.ResultJson.Should().Be("{\"patterns\":[]}");
        instance.ProcessingTimeMs.Should().Be(1200);
    }

    [Fact]
    public async Task AnalysisFailed_WithRetryAvailable_ShouldStayInProcessing()
    {
        var diagramId = Guid.NewGuid();

        await _harness.Bus.Publish(new DiagramUploadedEvent
        {
            DiagramId = diagramId,
            FileName = "test.puml",
            FileHash = "abc",
            StoragePath = "/s",
            Timestamp = DateTime.UtcNow
        });
        await _sagaHarness.Exists(diagramId, x => x.Processing);

        await _harness.Bus.Publish(new AnalysisFailedEvent
        {
            DiagramId = diagramId,
            AnalysisId = Guid.NewGuid(),
            ErrorMessage = "Transient error",
            FailedProviders = ["OpenAI"],
            Timestamp = DateTime.UtcNow
        });

        (await _harness.Consumed.Any<AnalysisFailedEvent>()).Should().BeTrue();

        var instance = _sagaHarness.Sagas.ContainsInState(diagramId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.Processing);
        instance.Should().NotBeNull();
        instance!.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task AnalysisFailed_AfterMaxRetries_ShouldTransitionToFailed()
    {
        var diagramId = Guid.NewGuid();

        await _harness.Bus.Publish(new DiagramUploadedEvent
        {
            DiagramId = diagramId,
            FileName = "test.puml",
            FileHash = "abc",
            StoragePath = "/s",
            Timestamp = DateTime.UtcNow
        });
        await _sagaHarness.Exists(diagramId, x => x.Processing);

        for (var i = 0; i < 3; i++)
        {
            await _harness.Bus.Publish(new AnalysisFailedEvent
            {
                DiagramId = diagramId,
                AnalysisId = Guid.NewGuid(),
                ErrorMessage = $"Retry attempt {i + 1}",
                FailedProviders = ["OpenAI"],
                Timestamp = DateTime.UtcNow
            });

            await _sagaHarness.Exists(diagramId, x => x.Processing);
        }

        await _harness.Bus.Publish(new AnalysisFailedEvent
        {
            DiagramId = diagramId,
            AnalysisId = Guid.NewGuid(),
            ErrorMessage = "Final failure after max retries",
            FailedProviders = ["OpenAI"],
            Timestamp = DateTime.UtcNow
        });

        var instanceId = await _sagaHarness.Exists(diagramId, x => x.Failed);
        instanceId.Should().NotBeNull();

        var instance = _sagaHarness.Sagas.ContainsInState(diagramId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.Failed);
        instance!.RetryCount.Should().Be(3);
        instance.ErrorMessage.Should().Be("Final failure after max retries");
    }

    [Fact]
    public async Task ReportGenerated_ShouldTransitionToCompleted()
    {
        var diagramId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        await _harness.Bus.Publish(new DiagramUploadedEvent
        {
            DiagramId = diagramId,
            FileName = "test.puml",
            FileHash = "abc",
            StoragePath = "/s",
            Timestamp = DateTime.UtcNow
        });
        await _sagaHarness.Exists(diagramId, x => x.Processing);

        await _harness.Bus.Publish(new AnalysisCompletedEvent
        {
            DiagramId = diagramId,
            AnalysisId = Guid.NewGuid(),
            ResultJson = "{}",
            ProvidersUsed = ["OpenAI"],
            ProcessingTimeMs = 500,
            Timestamp = DateTime.UtcNow
        });
        await _sagaHarness.Exists(diagramId, x => x.Analyzed);

        await _harness.Bus.Publish(new ReportGeneratedEvent
        {
            DiagramId = diagramId,
            AnalysisId = Guid.NewGuid(),
            ReportId = reportId,
            Timestamp = DateTime.UtcNow
        });

        var instanceId = await _sagaHarness.Exists(diagramId, x => x.Completed);
        instanceId.Should().NotBeNull();

        var instance = _sagaHarness.Sagas.ContainsInState(diagramId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.Completed);
        instance!.ReportId.Should().Be(reportId);
    }

    [Fact]
    public async Task ReportFailed_ShouldTransitionToFailed()
    {
        var diagramId = Guid.NewGuid();

        await _harness.Bus.Publish(new DiagramUploadedEvent
        {
            DiagramId = diagramId,
            FileName = "test.puml",
            FileHash = "abc",
            StoragePath = "/s",
            Timestamp = DateTime.UtcNow
        });
        await _sagaHarness.Exists(diagramId, x => x.Processing);

        await _harness.Bus.Publish(new AnalysisCompletedEvent
        {
            DiagramId = diagramId,
            AnalysisId = Guid.NewGuid(),
            ResultJson = "{}",
            ProvidersUsed = ["OpenAI"],
            ProcessingTimeMs = 500,
            Timestamp = DateTime.UtcNow
        });
        await _sagaHarness.Exists(diagramId, x => x.Analyzed);

        await _harness.Bus.Publish(new ReportFailedEvent
        {
            DiagramId = diagramId,
            AnalysisId = Guid.NewGuid(),
            ErrorMessage = "PDF generation failed",
            Timestamp = DateTime.UtcNow
        });

        var instanceId = await _sagaHarness.Exists(diagramId, x => x.Failed);
        instanceId.Should().NotBeNull();

        var instance = _sagaHarness.Sagas.ContainsInState(diagramId, _sagaHarness.StateMachine, _sagaHarness.StateMachine.Failed);
        instance!.ErrorMessage.Should().Be("PDF generation failed");
    }
}
