using FluentValidation;
using PxOperations.Api.Features.Milestones.Contracts;

namespace PxOperations.Api.Features.Milestones;

public sealed class UpdateMilestoneRequestValidator : AbstractValidator<UpdateMilestoneRequest>
{
    private static readonly string[] ValidTypes =
    [
        "apresentação sponsor", "apresentacao sponsor", "sponsor_presentation",
        "entrega final", "final_delivery",
        "presencial com cliente", "client_onsite",
        "kickoff",
        "outros", "other"
    ];

    public UpdateMilestoneRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0)
            .When(x => x.ProjectId.HasValue);

        RuleFor(x => x.Type)
            .Must(v => ValidTypes.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Type is not null)
            .WithMessage("Type must be one of: Apresentação Sponsor, Entrega Final, Presencial com Cliente, Kickoff, Outros.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Title is not null);

        RuleFor(x => x.Date)
            .Must(BeValidDate)
            .When(x => x.Date is not null)
            .WithMessage("Date must be a valid date in yyyy-MM-dd format.");

        RuleFor(x => x.Time)
            .Must(BeValidTime)
            .When(x => x.Time is not null)
            .WithMessage("Time must be a valid time in HH:mm format.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }

    private static bool BeValidDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", out _);

    private static bool BeValidTime(string? value) =>
        TimeOnly.TryParseExact(value, "HH:mm", out _);
}
