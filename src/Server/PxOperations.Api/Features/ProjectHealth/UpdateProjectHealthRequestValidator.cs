using FluentValidation;
using PxOperations.Api.Features.ProjectHealth.Contracts;

namespace PxOperations.Api.Features.ProjectHealth;

public sealed class UpdateProjectHealthRequestValidator : AbstractValidator<UpdateProjectHealthRequest>
{
    private static readonly string[] ValidRagValues =
    [
        "green", "verde",
        "yellow", "amarelo",
        "red", "vermelho"
    ];

    public UpdateProjectHealthRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .Must(o => !o.HasValue || o.Value > 0)
            .WithMessage("ProjectId must be greater than 0.");

        RuleFor(x => x.Week)
            .Must(o => !o.HasValue || BeValidDate(o.Value))
            .WithMessage("Week must be a valid date in yyyy-MM-dd format.");

        RuleFor(x => x.ReporterEmail)
            .Must(o => !o.HasValue || !string.IsNullOrWhiteSpace(o.Value))
            .WithMessage("ReporterEmail must not be empty.");

        RuleFor(x => x.PracticesCount)
            .Must(o => !o.HasValue || (o.Value >= 0 && o.Value <= 5))
            .WithMessage("PracticesCount must be between 0 and 5.");

        RuleFor(x => x.Scope)
            .Must(o => !o.HasValue || BeValidRag(o.Value))
            .WithMessage("Scope must be one of: green, verde, yellow, amarelo, red, vermelho.");

        RuleFor(x => x.Schedule)
            .Must(o => !o.HasValue || BeValidRag(o.Value))
            .WithMessage("Schedule must be one of: green, verde, yellow, amarelo, red, vermelho.");

        RuleFor(x => x.Quality)
            .Must(o => !o.HasValue || BeValidRag(o.Value))
            .WithMessage("Quality must be one of: green, verde, yellow, amarelo, red, vermelho.");

        RuleFor(x => x.Satisfaction)
            .Must(o => !o.HasValue || BeValidRag(o.Value))
            .WithMessage("Satisfaction must be one of: green, verde, yellow, amarelo, red, vermelho.");

        RuleFor(x => x.Highlights)
            .Must(o => !o.HasValue || !string.IsNullOrWhiteSpace(o.Value))
            .WithMessage("Highlights must not be empty.");
    }

    private static bool BeValidDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", out _);

    private static bool BeValidRag(string? value) =>
        value is not null && ValidRagValues.Contains(value, StringComparer.OrdinalIgnoreCase);
}
