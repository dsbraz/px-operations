using FluentValidation;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

public sealed class DismissNpsCollectionRequestValidator : AbstractValidator<DismissNpsCollectionRequest>
{
    public DismissNpsCollectionRequestValidator()
    {
        // F6 exige motivo: sem ele o card some do quadro sem explicação.
        RuleFor(r => r.Reason).NotEmpty().MaximumLength(500);
    }
}
