using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.Orchestrator.Application.Contracts.Interfaces;
using ArchLens.SharedKernel.Application;
using MediatR;

namespace ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.GetStatus;

public sealed class GetSagaStatusByDiagramHandler(ISagaStateRepository repository)
    : IRequestHandler<GetSagaStatusByDiagramQuery, Result<SagaStatusResponse>>
{
    public async Task<Result<SagaStatusResponse>> Handle(GetSagaStatusByDiagramQuery request, CancellationToken cancellationToken)
    {
        var result = await repository.GetByDiagramIdAsync(request.DiagramId, cancellationToken);

        if (result is null)
            return Result.Failure<SagaStatusResponse>(Error.NotFound);

        return result;
    }
}
