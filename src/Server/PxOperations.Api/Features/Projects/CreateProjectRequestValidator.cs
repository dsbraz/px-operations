using FluentValidation;
using PxOperations.Api.Features.Projects.Contracts;

namespace PxOperations.Api.Features.Projects;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    private static readonly string[] ValidDcs = ["DC1", "DC2", "DC3", "DC4", "DC5", "DC6"];
    private static readonly string[] ValidStatuses = ["em andamento", "in_progress", "programado", "scheduled", "encerrado", "closed"];
    private static readonly string[] ValidTypes = ["squad", "escopo fechado", "fixed_scope", "alocacao", "alocação", "staffing"];
    private static readonly string[] ValidRenewals = ["none", "pendente", "pending", "em andamento", "in_progress", "aprovada", "approved"];

    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Dc)
            .NotEmpty()
            .Must(v => ValidDcs.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Dc must be one of: DC1, DC2, DC3, DC4, DC5, DC6.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(v => ValidStatuses.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be one of: Em andamento, Programado, Encerrado.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Client)
            .MaximumLength(200);

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(v => ValidTypes.Contains(v, StringComparer.OrdinalIgnoreCase))
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
            .MaximumLength(200);

        RuleFor(x => x.Renewal)
            .Must(v => ValidRenewals.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Renewal is not null)
            .WithMessage("Renewal must be one of: None, Pendente, Em andamento, Aprovada.");

        RuleFor(x => x.RenewalObservation)
            .MaximumLength(500);
    }

    private static bool BeValidDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", out _);
}
