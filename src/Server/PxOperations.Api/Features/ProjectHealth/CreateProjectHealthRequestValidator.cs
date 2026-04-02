using FluentValidation;
using PxOperations.Api.Features.ProjectHealth.Contracts;

namespace PxOperations.Api.Features.ProjectHealth;

public sealed class CreateProjectHealthRequestValidator : AbstractValidator<CreateProjectHealthRequest>
{
    private static readonly string[] ValidRagValues =
    [
        "green", "verde",
        "yellow", "amarelo",
        "red", "vermelho"
    ];

    public CreateProjectHealthRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0);

        RuleFor(x => x.Week)
            .NotEmpty()
            .Must(BeValidDate)
            .WithMessage("Week must be a valid date in yyyy-MM-dd format.");

        RuleFor(x => x.ReporterEmail)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.PracticesCount)
            .InclusiveBetween(0, 5);

        RuleFor(x => x.Scope)
            .NotEmpty()
            .Must(BeValidRag)
            .WithMessage("Scope must be one of: green, verde, yellow, amarelo, red, vermelho.");

        RuleFor(x => x.Schedule)
            .NotEmpty()
            .Must(BeValidRag)
            .WithMessage("Schedule must be one of: green, verde, yellow, amarelo, red, vermelho.");

        RuleFor(x => x.Quality)
            .NotEmpty()
            .Must(BeValidRag)
            .WithMessage("Quality must be one of: green, verde, yellow, amarelo, red, vermelho.");

        RuleFor(x => x.Satisfaction)
            .NotEmpty()
            .Must(BeValidRag)
            .WithMessage("Satisfaction must be one of: green, verde, yellow, amarelo, red, vermelho.");

        RuleFor(x => x.Highlights)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.SubProject)
            .MaximumLength(200);

        RuleFor(x => x.ExpansionComment)
            .MaximumLength(500);
    }

    private static bool BeValidDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", out _);

    private static bool BeValidRag(string? value) =>
        value is not null && ValidRagValues.Contains(value, StringComparer.OrdinalIgnoreCase);
}
