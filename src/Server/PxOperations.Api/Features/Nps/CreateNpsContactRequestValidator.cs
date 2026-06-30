using FluentValidation;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

public sealed class CreateNpsContactRequestValidator : AbstractValidator<CreateNpsContactRequest>
{
    public CreateNpsContactRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(r => r.Role).MaximumLength(120);
    }
}
