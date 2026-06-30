using FluentValidation;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

public sealed class SubmitNpsSurveyResponseRequestValidator : AbstractValidator<SubmitNpsSurveyResponseRequest>
{
    public SubmitNpsSurveyResponseRequestValidator()
    {
        RuleFor(r => r.Score).InclusiveBetween(0, 10);
        RuleFor(r => r.Scope).InclusiveBetween(0, 10).When(r => r.Scope.HasValue);
        RuleFor(r => r.Schedule).InclusiveBetween(0, 10).When(r => r.Schedule.HasValue);
        RuleFor(r => r.Quality).InclusiveBetween(0, 10).When(r => r.Quality.HasValue);
        RuleFor(r => r.Communication).InclusiveBetween(0, 10).When(r => r.Communication.HasValue);
        RuleFor(r => r.Tags).MaximumLength(500);
        RuleFor(r => r.Comment).MaximumLength(2000);
        RuleFor(r => r.RespondentName).MaximumLength(200);
        RuleFor(r => r.RespondentEmail).EmailAddress().MaximumLength(320).When(r => !string.IsNullOrWhiteSpace(r.RespondentEmail));
    }
}
