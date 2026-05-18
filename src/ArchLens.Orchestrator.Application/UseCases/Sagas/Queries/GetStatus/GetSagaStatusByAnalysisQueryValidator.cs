using FluentValidation;

namespace ArchLens.Orchestrator.Application.UseCases.Sagas.Queries.GetStatus;

public sealed class GetSagaStatusByAnalysisQueryValidator : AbstractValidator<GetSagaStatusByAnalysisQuery>
{
    public GetSagaStatusByAnalysisQueryValidator()
    {
        RuleFor(x => x.AnalysisId)
            .NotEmpty()
            .WithMessage("AnalysisId is required.");
    }
}
