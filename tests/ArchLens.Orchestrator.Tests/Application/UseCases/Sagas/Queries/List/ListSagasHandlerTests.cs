using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.List;
using FluentAssertions;
using NSubstitute;

namespace ArchLens.Orchestrator.Tests.Application.UseCases.Sagas.Queries.List;

public class ListSagasHandlerTests
{
    private readonly ISagaStateRepository _repository;
    private readonly ListSagasHandler _sut;

    public ListSagasHandlerTests()
    {
        _repository = Substitute.For<ISagaStateRepository>();
        _sut = new ListSagasHandler(_repository);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessWithItems()
    {
        // Arrange
        var items = new List<SagaStatusResponse>
        {
            CreateSagaStatusResponse(),
            CreateSagaStatusResponse()
        };
        _repository.ListAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns(items.AsReadOnly());

        var query = new ListSagasQuery(1, 20);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyListWhenNoItems()
    {
        // Arrange
        var items = new List<SagaStatusResponse>();
        _repository.ListAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns(items.AsReadOnly());

        var query = new ListSagasQuery(1, 20);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldPassCorrectPageAndPageSize()
    {
        // Arrange
        var items = new List<SagaStatusResponse>();
        _repository.ListAsync(3, 10, Arg.Any<CancellationToken>())
            .Returns(items.AsReadOnly());

        var query = new ListSagasQuery(3, 10);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _repository.Received(1).ListAsync(3, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectPageMetadata()
    {
        // Arrange
        var items = new List<SagaStatusResponse>
        {
            CreateSagaStatusResponse(),
            CreateSagaStatusResponse(),
            CreateSagaStatusResponse()
        };
        _repository.ListAsync(2, 10, Arg.Any<CancellationToken>())
            .Returns(items.AsReadOnly());

        var query = new ListSagasQuery(2, 10);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithDefaultValues_ShouldUseDefaults()
    {
        // Arrange
        var items = new List<SagaStatusResponse>();
        _repository.ListAsync(1, 20, Arg.Any<CancellationToken>())
            .Returns(items.AsReadOnly());

        var query = new ListSagasQuery();

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _repository.Received(1).ListAsync(1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var items = new List<SagaStatusResponse>();
        _repository.ListAsync(1, 20, ct)
            .Returns(items.AsReadOnly());

        var query = new ListSagasQuery();

        // Act
        await _sut.Handle(query, ct);

        // Assert
        await _repository.Received(1).ListAsync(1, 20, ct);
    }

    [Theory]
    [InlineData(0, 20, 1)]
    [InlineData(-1, 20, 1)]
    public async Task Handle_WithInvalidPage_ShouldClampToMinimum(int inputPage, int pageSize, int expectedPage)
    {
        // Arrange - PagedRequest clamps page < 1 to 1
        var items = new List<SagaStatusResponse>();
        _repository.ListAsync(expectedPage, pageSize, Arg.Any<CancellationToken>())
            .Returns(items.AsReadOnly());

        var query = new ListSagasQuery(inputPage, pageSize);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _repository.Received(1).ListAsync(expectedPage, pageSize, Arg.Any<CancellationToken>());
    }

    private static SagaStatusResponse CreateSagaStatusResponse()
    {
        return new SagaStatusResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
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
