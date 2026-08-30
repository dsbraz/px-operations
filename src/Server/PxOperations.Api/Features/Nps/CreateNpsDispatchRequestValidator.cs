using FluentValidation;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

public sealed class CreateNpsDispatchRequestValidator : AbstractValidator<CreateNpsDispatchRequest>
{
    public CreateNpsDispatchRequestValidator()
    {
        RuleFor(request => request.ProjectId).GreaterThan(0);
        RuleFor(request => request.Format).Must(value => value is "complete" or "simplified");
        RuleFor(request => request.Language).Must(value => value is "pt" or "en" or "es");
    }
}
