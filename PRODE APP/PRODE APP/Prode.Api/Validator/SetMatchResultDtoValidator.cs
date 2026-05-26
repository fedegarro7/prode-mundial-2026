using FluentValidation;
using Prode.Api.DTOs;

namespace Prode.Api.Validators;

public class SetMatchResultDtoValidator
    : AbstractValidator<SetMatchResultDto>
{
    public SetMatchResultDtoValidator()
    {
        RuleFor(x => x.HomeScore)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(20);

        RuleFor(x => x.AwayScore)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(20);
    }
}