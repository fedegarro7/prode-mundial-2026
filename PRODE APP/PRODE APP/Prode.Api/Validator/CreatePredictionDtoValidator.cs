using FluentValidation;
using Prode.Api.DTOs;

namespace Prode.Api.Validators;

public class CreatePredictionDtoValidator
    : AbstractValidator<CreatePredictionDto>
{
    public CreatePredictionDtoValidator()
    {
        RuleFor(x => x.MatchId)
            .GreaterThan(0);

        RuleFor(x => x.HomeScorePrediction)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(20);

        RuleFor(x => x.AwayScorePrediction)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(20);
    }
}