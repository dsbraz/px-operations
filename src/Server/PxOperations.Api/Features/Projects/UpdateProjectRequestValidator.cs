using FluentValidation;

namespace PxOperations.Api.Features.Projects;

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    private static readonly string[] ValidDcs = ["DC1", "DC2", "DC3", "DC4", "DC5", "DC6"];
    private static readonly string[] ValidStatuses = ["em andamento", "in_progress", "programado", "scheduled", "encerrado", "closed"];
    private static readonly string[] ValidTypes = ["squad", "escopo fechado", "fixed_scope", "alocacao", "alocação", "staffing"];
    private static readonly string[] ValidRenewals = ["none", "pendente", "pending", "em andamento", "in_progress", "aprovada", "approved"];

    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Dc)
            .Must(v => ValidDcs.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Dc is not null)
            .WithMessage("Dc must be one of: DC1, DC2, DC3, DC4, DC5, DC6.");

        RuleFor(x => x.Status)
            .Must(v => ValidStatuses.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Status is not null)
            .WithMessage("Status must be one of: Em andamento, Programado, Encerrado.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.Client)
            .MaximumLength(200)
            .When(x => x.Client is not null);

        RuleFor(x => x.Type)
            .Must(v => ValidTypes.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Type is not null)
            .WithMessage("Type must be one of: Squad, Escopo Fechado, Alocação.");

        RuleFor(x => x.StartDate)
            .Must(BeValidDate)
            .When(x => x.StartDate is not null)
            .WithMessage("StartDate must be a valid date in yyyy-MM-dd format.");

        RuleFor(x => x.EndDate)
            .Must(BeValidDate)
            .When(x => x.EndDate is not null)
            .WithMessage("EndDate must be a valid date in yyyy-MM-dd format.");

        RuleFor(x => x.DeliveryManager)
            .MaximumLength(200)
            .When(x => x.DeliveryManager is not null);

        RuleFor(x => x.Renewal)
            .Must(v => ValidRenewals.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Renewal is not null)
            .WithMessage("Renewal must be one of: None, Pendente, Em andamento, Aprovada.");

        RuleFor(x => x.RenewalObservation)
            .MaximumLength(500)
            .When(x => x.RenewalObservation is not null);
    }

    private static bool BeValidDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", out _);
}
