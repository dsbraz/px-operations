using System.Globalization;
using FluentValidation;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

public sealed class NpsQueryRequestValidator : AbstractValidator<NpsQueryRequest>
{
    private static readonly string[] Statuses = ["responded", "link_generated", "pending"];
    private static readonly string[] Formats = ["complete", "simplified"];
    private static readonly string[] Classifications = ["detractor", "passive", "promoter"];
    private static readonly string[] ProjectTypes = ["squad", "fixed_scope", "staffing"];

    public NpsQueryRequestValidator()
    {
        RuleFor(request => request.Search).MaximumLength(200);
        RuleForEach(request => request.Dc).Must(value =>
            new[] { "dc1", "dc2", "dc3", "dc4", "dc5", "dc6" }
                .Contains(value, StringComparer.OrdinalIgnoreCase)).WithMessage("Invalid delivery center.");
        RuleForEach(request => request.ProjectType).Must(value => ProjectTypes.Contains(value, StringComparer.OrdinalIgnoreCase));
        RuleForEach(request => request.Status).Must(value => Statuses.Contains(value, StringComparer.OrdinalIgnoreCase));
        RuleForEach(request => request.Format).Must(value => Formats.Contains(value, StringComparer.OrdinalIgnoreCase));
        RuleForEach(request => request.Classification).Must(value => Classifications.Contains(value, StringComparer.OrdinalIgnoreCase));
        RuleFor(request => request.From).Must(BeDate).When(request => request.From is not null).WithMessage("from must use yyyy-MM-dd.");
        RuleFor(request => request.To).Must(BeDate).When(request => request.To is not null).WithMessage("to must use yyyy-MM-dd.");
        RuleFor(request => request).Must(request =>
        {
            if (!TryDate(request.From, out var from) || !TryDate(request.To, out var to))
            {
                return true;
            }

            return from <= to;
        }).WithMessage("from must not be after to.");
    }

    private static bool BeDate(string? value) => TryDate(value, out _);

    private static bool TryDate(string? value, out DateOnly date)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
