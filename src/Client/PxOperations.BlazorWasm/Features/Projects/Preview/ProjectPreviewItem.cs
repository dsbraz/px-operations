using System.Globalization;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Projects.Preview;

public sealed record ProjectPreviewItem(
    int Id,
    string DeliveryCenter,
    string Status,
    string Name,
    string Client,
    string Type,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string DeliveryManager,
    string Renewal,
    string RenewalObservation)
{
    private static readonly CultureInfo BrazilianPortuguese =
        CultureInfo.GetCultureInfo("pt-BR");

    public string StartDateLabel => FormatDate(StartDate);

    public string EndDateLabel => FormatDate(EndDate);

    public string RenewalLabel => Renewal == "None" ? "Sem renovação" : Renewal;

    public bool IsExpiringWithin(DateOnly start, int days)
    {
        if (EndDate is null)
            return false;

        var remaining = EndDate.Value.DayNumber - start.DayNumber;
        return remaining >= 0 && remaining <= days;
    }

    public static ProjectPreviewItem From(ProjectResponse project) => new(
        Id: project.Id,
        DeliveryCenter: RequiredOrFallback(project.Dc),
        Status: RequiredOrFallback(project.Status),
        Name: RequiredOrFallback(project.Name),
        Client: OptionalOrFallback(project.Client),
        Type: RequiredOrFallback(project.Type),
        StartDate: ParseDate(project.StartDate),
        EndDate: ParseDate(project.EndDate),
        DeliveryManager: OptionalOrFallback(project.DeliveryManager),
        Renewal: RequiredOrFallback(project.Renewal),
        RenewalObservation: OptionalOrFallback(project.RenewalObservation));

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;

    private static string FormatDate(DateOnly? date) =>
        date?.ToString("dd/MM/yyyy", BrazilianPortuguese) ?? "Não informado";

    private static string RequiredOrFallback(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Não informado" : value.Trim();

    private static string OptionalOrFallback(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Não informado" : value.Trim();
}
