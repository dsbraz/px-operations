using System.Globalization;
using PxOperations.Api.Features.Nps.Contracts;
using PxOperations.Application.Features.Nps;

namespace PxOperations.Api.Features.Nps;

/// <summary>
/// Traduz a requisição de consulta no filtro que a camada de aplicação entende,
/// como os demais módulos fazem em seus *Mappings. O formato da data já foi
/// garantido por NpsQueryRequestValidator antes de chegar aqui.
/// </summary>
public static class NpsMappings
{
    public static NpsFilter ToFilter(NpsQueryRequest request)
        => new(
            request.Search?.Trim(),
            request.Client,
            request.Dc,
            request.ProjectType,
            request.DeliveryManager,
            request.Status,
            request.Format,
            request.Classification,
            ParseDate(request.From),
            ParseDate(request.To),
            request.IncludeWaived,
            request.ProjectId);

    private static DateOnly? ParseDate(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
