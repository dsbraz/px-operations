using FluentValidation;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

public sealed class CreateNpsContactRequestValidator : AbstractValidator<CreateNpsContactRequest>
{
    public CreateNpsContactRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Role).MaximumLength(120);
    }
}
