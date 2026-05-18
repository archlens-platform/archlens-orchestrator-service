using ArchLens.Orchestrator.Application.Contracts.DTOs.SagaDTOs;
using ArchLens.SharedKernel.Application;
using MediatR;

namespace ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.List;

public record ListSagasQuery(int Page = 1, int PageSize = 20, string? UserId = null, bool IsAdmin = false) : IRequest<Result<PagedResponse<SagaStatusResponse>>>;
