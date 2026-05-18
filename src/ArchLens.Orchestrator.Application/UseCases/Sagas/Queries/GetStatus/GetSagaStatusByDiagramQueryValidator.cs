using FluentValidation;

namespace ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.GetStatus;

public sealed class GetSagaStatusByDiagramQueryValidator : AbstractValidator<GetSagaStatusByDiagramQuery>
{
    public GetSagaStatusByDiagramQueryValidator()
    {
        RuleFor(x => x.DiagramId)
            .NotEmpty()
            .WithMessage("DiagramId is required.");
    }
}
