using FluentValidation;
using PxOperations.Api.Features.Projects.Contracts;

namespace PxOperations.Api.Features.Projects;

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    private static readonly string[] ValidDcs = ["DC1", "DC2", "DC3", "DC4", "DC5", "DC6"];
    private static readonly string[] ValidStatuses = ["em andamento", "in_progress", "programado", "scheduled", "encerrado", "closed"];
    private static readonly string[] ValidTypes = ["squad", "escopo fechado", "fixed_scope", "alocacao", "alocação", "staffing"];
    private static readonly string[] ValidRenewals = ["none", "pendente", "pending", "em andamento", "in_progress", "aprovada", "approved"];

    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Dc.Value)
            .Must(v => ValidDcs.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Dc.HasValue)
            .WithMessage("Dc must be one of: DC1, DC2, DC3, DC4, DC5, DC6.");

        RuleFor(x => x.Status.Value)
            .Must(v => ValidStatuses.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Status.HasValue)
            .WithMessage("Status must be one of: Em andamento, Programado, Encerrado.");

        RuleFor(x => x.Name.Value)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Name.HasValue);

        RuleFor(x => x.Client.Value)
            .MaximumLength(200)
            .When(x => x.Client.HasValue && x.Client.Value is not null);

        RuleFor(x => x.Type.Value)
            .Must(v => ValidTypes.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Type.HasValue)
            .WithMessage("Type must be one of: Squad, Escopo Fechado, Alocação.");

        RuleFor(x => x.StartDate.Value)
            .Must(BeValidDate)
            .When(x => x.StartDate.HasValue && x.StartDate.Value is not null)
            .WithMessage("StartDate must be a valid date in yyyy-MM-dd format.");

        RuleFor(x => x.EndDate.Value)
            .Must(BeValidDate)
            .When(x => x.EndDate.HasValue && x.EndDate.Value is not null)
            .WithMessage("EndDate must be a valid date in yyyy-MM-dd format.");

        RuleFor(x => x.DeliveryManager.Value)
            .MaximumLength(200)
            .When(x => x.DeliveryManager.HasValue && x.DeliveryManager.Value is not null);

        RuleFor(x => x.Renewal.Value)
            .Must(v => ValidRenewals.Contains(v!, StringComparer.OrdinalIgnoreCase))
            .When(x => x.Renewal.HasValue)
            .WithMessage("Renewal must be one of: None, Pendente, Em andamento, Aprovada.");

        RuleFor(x => x.RenewalObservation.Value)
            .MaximumLength(500)
            .When(x => x.RenewalObservation.HasValue && x.RenewalObservation.Value is not null);
    }

    private static bool BeValidDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", out _);
}
