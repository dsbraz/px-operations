using FluentValidation;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

public sealed class SubmitNpsSurveyResponseRequestValidator : AbstractValidator<SubmitNpsSurveyResponseRequest>
{
    public SubmitNpsSurveyResponseRequestValidator()
    {
        RuleFor(request => request.Score).InclusiveBetween(1, 10);
        RuleFor(request => request.Quality).InclusiveBetween(1, 5).When(request => request.Quality.HasValue);
        RuleFor(request => request.Schedule).InclusiveBetween(1, 5).When(request => request.Schedule.HasValue);
        RuleFor(request => request.Communication).InclusiveBetween(1, 5).When(request => request.Communication.HasValue);
        RuleFor(request => request.BusinessValue).InclusiveBetween(1, 5).When(request => request.BusinessValue.HasValue);
        RuleFor(request => request.Comment).MaximumLength(2000);
        RuleFor(request => request.RespondentName).MaximumLength(200);
        RuleFor(request => request.RespondentEmail).EmailAddress().MaximumLength(320)
            .When(request => !string.IsNullOrWhiteSpace(request.RespondentEmail));
    }
}
