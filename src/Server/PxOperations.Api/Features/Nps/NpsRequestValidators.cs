using System.Globalization;
using FluentValidation;

namespace PxOperations.Api.Features.Nps;

public sealed class NpsQueryRequestValidator : AbstractValidator<NpsQueryRequest>
{
    private static readonly string[] Statuses = ["no_link", "awaiting_response", "recollection", "current", "waived"];
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

public sealed class CreateNpsContactRequestValidator : AbstractValidator<CreateNpsContactRequest>
{
    public CreateNpsContactRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Role).MaximumLength(120);
    }
}

public sealed class UpdateNpsContactRequestValidator : AbstractValidator<UpdateNpsContactRequest>
{
    public UpdateNpsContactRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Role).MaximumLength(120);
    }
}

public sealed class CreateNpsDispatchRequestValidator : AbstractValidator<CreateNpsDispatchRequest>
{
    public CreateNpsDispatchRequestValidator()
    {
        RuleFor(request => request.ProjectId).GreaterThan(0);
        RuleFor(request => request.Format).Must(value => value is "complete" or "simplified");
        RuleFor(request => request.Language).Must(value => value is "pt" or "en" or "es");
    }
}

public sealed class WaiveNpsCollectionRequestValidator : AbstractValidator<WaiveNpsCollectionRequest>
{
    public WaiveNpsCollectionRequestValidator()
    {
        RuleFor(request => request.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class SubmitNpsSurveyResponseRequestValidator : AbstractValidator<SubmitNpsSurveyResponseRequest>
{
    public SubmitNpsSurveyResponseRequestValidator()
    {
        RuleFor(request => request.Score).InclusiveBetween(1, 10);
        RuleFor(request => request.Quality).InclusiveBetween(1, 5).When(request => request.Quality.HasValue);
        RuleFor(request => request.Schedule).InclusiveBetween(1, 5).When(request => request.Schedule.HasValue);
        RuleFor(request => request.Communication).InclusiveBetween(1, 5).When(request => request.Communication.HasValue);
        RuleFor(request => request.BusinessValue).InclusiveBetween(1, 5).When(request => request.BusinessValue.HasValue);
        RuleFor(request => request.Comment).MaximumLength(2000);
        RuleFor(request => request.RespondentName).MaximumLength(200);
        RuleFor(request => request.RespondentEmail).EmailAddress().MaximumLength(320)
            .When(request => !string.IsNullOrWhiteSpace(request.RespondentEmail));
    }
}
