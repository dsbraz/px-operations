using FluentValidation;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

public sealed class CreateNpsDispatchRequestValidator : AbstractValidator<CreateNpsDispatchRequest>
{
    public CreateNpsDispatchRequestValidator()
    {
        RuleFor(r => r.ProjectId).GreaterThan(0);
        RuleFor(r => r.PeriodStart).Must(BeDate).WithMessage("PeriodStart must be a valid date.");
        RuleFor(r => r.PeriodEnd).Must(BeDate).WithMessage("PeriodEnd must be a valid date.");
        RuleFor(r => r.Format).Must(v => Try(() => NpsMappings.ParseFormFormat(v))).WithMessage("Format is invalid.");
        RuleFor(r => r.Language).Must(v => Try(() => NpsMappings.ParseLanguage(v))).WithMessage("Language is invalid.");
        RuleFor(r => r.CreatedBy).NotEmpty().MaximumLength(200);
    }

    private static bool BeDate(string value) => DateOnly.TryParse(value, out _);
    private static bool Try(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
