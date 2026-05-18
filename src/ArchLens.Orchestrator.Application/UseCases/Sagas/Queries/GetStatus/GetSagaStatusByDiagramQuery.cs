using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.SharedKernel.Application;
using MediatR;

namespace ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.GetStatus;

public record GetSagaStatusByDiagramQuery(Guid DiagramId) : IRequest<Result<SagaStatusResponse>>;
