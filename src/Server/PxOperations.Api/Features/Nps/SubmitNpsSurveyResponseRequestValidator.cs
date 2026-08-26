using FluentValidation;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

public sealed class SubmitNpsSurveyResponseRequestValidator : AbstractValidator<SubmitNpsSurveyResponseRequest>
{
    public SubmitNpsSurveyResponseRequestValidator()
    {
        RuleFor(r => r.Score).InclusiveBetween(1, 10);
        RuleFor(r => r.BusinessValue).InclusiveBetween(1, 5).When(r => r.BusinessValue.HasValue);
        RuleFor(r => r.Schedule).InclusiveBetween(1, 5).When(r => r.Schedule.HasValue);
        RuleFor(r => r.Quality).InclusiveBetween(1, 5).When(r => r.Quality.HasValue);
        RuleFor(r => r.Communication).InclusiveBetween(1, 5).When(r => r.Communication.HasValue);
        RuleFor(r => r.Tags).MaximumLength(500);
        RuleFor(r => r.Comment).MaximumLength(2000);
        RuleFor(r => r.RespondentName).MaximumLength(200);
        RuleFor(r => r.RespondentEmail).EmailAddress().MaximumLength(320).When(r => !string.IsNullOrWhiteSpace(r.RespondentEmail));
    }
}
