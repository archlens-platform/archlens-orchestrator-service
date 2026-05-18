using ArchLens.Orchestrator.Infrastructure.Persistence;
using ArchLens.Orchestrator.Infrastructure.Persistence.EFCore.Context;
using ArchLens.Orchestrator.Infrastructure.Saga;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Orchestrator.Tests.Infrastructure.Persistence;

public class UnitOfWorkTests : IDisposable
{
    private readonly SagaDbContext _context;
    private readonly UnitOfWork _sut;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<SagaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SagaDbContext(options);
        _sut = new UnitOfWork(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_WithTrackedEntity_ShouldPersistChanges()
    {
        // Arrange
        var state = CreateSagaState();
        _context.SagaStates.Add(state);

        // Act
        var result = await _sut.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        var persisted = await _context.SagaStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CorrelationId == state.CorrelationId);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ShouldReturnZero()
    {
        // Act
        var result = await _sut.SaveChangesAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntities_ShouldPersistAll()
    {
        // Arrange
        _context.SagaStates.AddRange(
            CreateSagaState(),
            CreateSagaState(),
            CreateSagaState());

        // Act
        var result = await _sut.SaveChangesAsync();

        // Assert
        result.Should().Be(3);
        var count = await _context.SagaStates.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _context.SagaStates.Add(CreateSagaState());

        // Act
        var act = () => _sut.SaveChangesAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SaveChangesAsync_WithModifiedEntity_ShouldPersistUpdate()
    {
        // Arrange
        var state = CreateSagaState();
        _context.SagaStates.Add(state);
        await _context.SaveChangesAsync();

        state.CurrentState = "Completed";
        state.ProcessingTimeMs = 5000;

        // Act
        var result = await _sut.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        var updated = await _context.SagaStates
            .AsNoTracking()
            .FirstAsync(x => x.CorrelationId == state.CorrelationId);
        updated.CurrentState.Should().Be("Completed");
        updated.ProcessingTimeMs.Should().Be(5000);
    }

    #endregion

    #region Helpers

    private static AnalysisSagaState CreateSagaState()
    {
        var now = DateTime.UtcNow;
        return new AnalysisSagaState
        {
            CorrelationId = Guid.NewGuid(),
            CurrentState = "Processing",
            AnalysisId = Guid.NewGuid(),
            DiagramId = Guid.NewGuid(),
            FileName = "test.puml",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    #endregion
}

public class UnitOfWorkExecuteAsyncTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SagaDbContext _context;
    private readonly UnitOfWork _sut;

    public UnitOfWorkExecuteAsyncTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SagaDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new SagaDbContext(options);
        _context.Database.EnsureCreated();
        _sut = new UnitOfWork(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    #region ExecuteAsync (void)

    [Fact]
    public async Task ExecuteAsync_WhenWorkSucceeds_ShouldCommitTransaction()
    {
        var sagaId = Guid.NewGuid();

        await _sut.ExecuteAsync(async ct =>
        {
            _context.SagaStates.Add(new AnalysisSagaState
            {
                CorrelationId = sagaId,
                CurrentState = "Processing",
                AnalysisId = Guid.NewGuid(),
                DiagramId = Guid.NewGuid(),
                FileName = "commit-test.puml",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await Task.CompletedTask;
        });

        var persisted = await _context.SagaStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CorrelationId == sagaId);
        persisted.Should().NotBeNull();
        persisted!.FileName.Should().Be("commit-test.puml");
    }

    [Fact]
    public async Task ExecuteAsync_WhenWorkThrows_ShouldRollbackAndPropagate()
    {
        var sagaId = Guid.NewGuid();

        var act = () => _sut.ExecuteAsync(async ct =>
        {
            _context.SagaStates.Add(new AnalysisSagaState
            {
                CorrelationId = sagaId,
                CurrentState = "Processing",
                AnalysisId = Guid.NewGuid(),
                DiagramId = Guid.NewGuid(),
                FileName = "rollback-test.puml",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await Task.CompletedTask;
            throw new InvalidOperationException("Simulated failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated failure");

        var persisted = await _context.SagaStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CorrelationId == sagaId);
        persisted.Should().BeNull();
    }

    #endregion

    #region ExecuteAsync<T> (with return value)

    [Fact]
    public async Task ExecuteAsyncT_WhenWorkSucceeds_ShouldCommitAndReturnResult()
    {
        var sagaId = Guid.NewGuid();

        var result = await _sut.ExecuteAsync<Guid>(async ct =>
        {
            _context.SagaStates.Add(new AnalysisSagaState
            {
                CorrelationId = sagaId,
                CurrentState = "Analyzed",
                AnalysisId = Guid.NewGuid(),
                DiagramId = Guid.NewGuid(),
                FileName = "return-test.puml",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await Task.CompletedTask;
            return sagaId;
        });

        result.Should().Be(sagaId);

        var persisted = await _context.SagaStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CorrelationId == sagaId);
        persisted.Should().NotBeNull();
        persisted!.CurrentState.Should().Be("Analyzed");
    }

    [Fact]
    public async Task ExecuteAsyncT_WhenWorkThrows_ShouldRollbackAndPropagate()
    {
        var sagaId = Guid.NewGuid();

        var act = () => _sut.ExecuteAsync<int>(async ct =>
        {
            _context.SagaStates.Add(new AnalysisSagaState
            {
                CorrelationId = sagaId,
                CurrentState = "Processing",
                AnalysisId = Guid.NewGuid(),
                DiagramId = Guid.NewGuid(),
                FileName = "rollback-generic-test.puml",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await Task.CompletedTask;
            throw new InvalidOperationException("Generic failure");
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Generic failure");

        var persisted = await _context.SagaStates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CorrelationId == sagaId);
        persisted.Should().BeNull();
    }

    #endregion
}
