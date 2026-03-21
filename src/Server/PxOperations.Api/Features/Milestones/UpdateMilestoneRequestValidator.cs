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
        RuleFor(x => x.ProjectId.Value)
            .GreaterThan(0)
            .When(x => x.ProjectId.HasValue);

        RuleFor(x => x.Type.Value)
            .Must(v => ValidTypes.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Type.HasValue)
            .WithMessage("Type must be one of: Apresentação Sponsor, Entrega Final, Presencial com Cliente, Kickoff, Outros.");

        RuleFor(x => x.Title.Value)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Title.HasValue);

        RuleFor(x => x.Date.Value)
            .Must(BeValidDate)
            .When(x => x.Date.HasValue)
            .WithMessage("Date must be a valid date in yyyy-MM-dd format.");

        RuleFor(x => x.Time.Value)
            .Must(BeValidTime)
            .When(x => x.Time.HasValue && x.Time.Value is not null)
            .WithMessage("Time must be a valid time in HH:mm format.");

        RuleFor(x => x.Notes.Value)
            .MaximumLength(1000)
            .When(x => x.Notes.HasValue && x.Notes.Value is not null);
    }

    private static bool BeValidDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", out _);

    private static bool BeValidTime(string? value) =>
        TimeOnly.TryParseExact(value, "HH:mm", out _);
}
