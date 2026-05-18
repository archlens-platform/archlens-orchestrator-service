using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.SharedKernel.Application;
using MediatR;

namespace ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.List;

public sealed class ListSagasHandler(ISagaStateRepository repository)
    : IRequestHandler<ListSagasQuery, Result<PagedResponse<SagaStatusResponse>>>
{
    public async Task<Result<PagedResponse<SagaStatusResponse>>> Handle(
        ListSagasQuery request,
        CancellationToken cancellationToken)
    {
        var paged = new PagedRequest(request.Page, request.PageSize);
        var items = await repository.ListAsync(paged.Page, paged.PageSize, request.IsAdmin ? null : request.UserId, cancellationToken);

        return new PagedResponse<SagaStatusResponse>(items, paged.Page, paged.PageSize, items.Count);
    }
}
