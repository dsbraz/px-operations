using FluentValidation;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

public sealed class WaiveNpsCollectionRequestValidator : AbstractValidator<WaiveNpsCollectionRequest>
{
    public WaiveNpsCollectionRequestValidator()
    {
        RuleFor(request => request.Reason).NotEmpty().MaximumLength(500);
    }
}
