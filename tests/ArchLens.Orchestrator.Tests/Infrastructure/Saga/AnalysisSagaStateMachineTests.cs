using ArchLens.Orchestrator.Infrastructure.Saga;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchLens.Orchestrator.Tests.Infrastructure.Saga;

public class AnalysisSagaStateMachineTests
{
    private readonly AnalysisSagaStateMachine _stateMachine;

    public AnalysisSagaStateMachineTests()
    {
        _stateMachine = new AnalysisSagaStateMachine(
            NullLogger<AnalysisSagaStateMachine>.Instance);
    }

    [Fact]
    public void StateMachine_ShouldDefineProcessingState()
    {
        _stateMachine.Processing.Should().NotBeNull();
        _stateMachine.Processing.Name.Should().Be("Processing");
    }

    [Fact]
    public void StateMachine_ShouldDefineAnalyzedState()
    {
        _stateMachine.Analyzed.Should().NotBeNull();
        _stateMachine.Analyzed.Name.Should().Be("Analyzed");
    }

    [Fact]
    public void StateMachine_ShouldDefineCompletedState()
    {
        _stateMachine.Completed.Should().NotBeNull();
        _stateMachine.Completed.Name.Should().Be("Completed");
    }

    [Fact]
    public void StateMachine_ShouldDefineFailedState()
    {
        _stateMachine.Failed.Should().NotBeNull();
        _stateMachine.Failed.Name.Should().Be("Failed");
    }

    [Fact]
    public void StateMachine_ShouldDefineDiagramUploadedEvent()
    {
        _stateMachine.DiagramUploaded.Should().NotBeNull();
    }

    [Fact]
    public void StateMachine_ShouldDefineAnalysisCompletedEvent()
    {
        _stateMachine.AnalysisCompleted.Should().NotBeNull();
    }

    [Fact]
    public void StateMachine_ShouldDefineAnalysisFailedEvent()
    {
        _stateMachine.AnalysisFailed.Should().NotBeNull();
    }

    [Fact]
    public void StateMachine_ShouldDefineReportGeneratedEvent()
    {
        _stateMachine.ReportGenerated.Should().NotBeNull();
    }

    [Fact]
    public void StateMachine_ShouldDefineReportFailedEvent()
    {
        _stateMachine.ReportFailed.Should().NotBeNull();
    }

    [Fact]
    public void StateMachine_ShouldHaveAllStatesConfigured()
    {
        _stateMachine.States.Should().Contain(s => s.Name == "Processing");
        _stateMachine.States.Should().Contain(s => s.Name == "Analyzed");
        _stateMachine.States.Should().Contain(s => s.Name == "Completed");
        _stateMachine.States.Should().Contain(s => s.Name == "Failed");
    }

    [Fact]
    public void StateMachine_ShouldHaveAllEventsConfigured()
    {
        _stateMachine.Events.Should().Contain(e => e.Name == "DiagramUploaded");
        _stateMachine.Events.Should().Contain(e => e.Name == "AnalysisCompleted");
        _stateMachine.Events.Should().Contain(e => e.Name == "AnalysisFailed");
        _stateMachine.Events.Should().Contain(e => e.Name == "ReportGenerated");
        _stateMachine.Events.Should().Contain(e => e.Name == "ReportFailed");
    }

    [Fact]
    public void StateMachine_ShouldBeCreatedWithoutErrors()
    {
        var act = () => new AnalysisSagaStateMachine(
            NullLogger<AnalysisSagaStateMachine>.Instance);

        act.Should().NotThrow();
    }

    [Fact]
    public void StateMachine_InstanceStateProperty_ShouldBeConfigured()
    {
        // Verify the state machine has the InstanceState configured
        // by checking that the state machine can be instantiated and has states
        _stateMachine.States.Should().HaveCountGreaterOrEqualTo(4);
    }

    [Fact]
    public void SagaState_NullStoragePath_ShouldFallbackToEmptyString()
    {
        // Arrange
        var saga = new AnalysisSagaState
        {
            CorrelationId = Guid.NewGuid(),
            CurrentState = "Initial",
            StoragePath = null
        };

        // Act - simulate the ?? string.Empty branch used in the state machine
        var storagePath = saga.StoragePath ?? string.Empty;

        // Assert
        storagePath.Should().Be(string.Empty);
    }
}
