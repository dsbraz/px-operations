using System.Globalization;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

/// <summary>
/// Formatações que mais de uma tela do NPS usa. Fica fora das páginas porque
/// os indicadores, a tabela de resultados e o detalhe da coleta precisam da
/// mesma regra de exibição — e o travessão de "sem valor" é uma delas.
/// </summary>
internal static class NpsDisplay
{
    // Uma cultura só para todos os números do módulo. Misturar a do navegador
    // com pt-BR fazia a mesma tela mostrar "33.3" no indicador e "4,2" na média
    // por aspecto, logo abaixo.
    internal static readonly CultureInfo OperationCulture = CultureInfo.GetCultureInfo("pt-BR");

    internal static string Metric(double? value)
        => value?.ToString("0.0", OperationCulture) ?? "—";

    internal static string Count(int? value)
        => value?.ToString(OperationCulture) ?? "—";

    internal static string AuthorName(NpsResponseView response)
    {
        if (!string.IsNullOrWhiteSpace(response.RespondentName))
        {
            return response.RespondentName;
        }

        if (!string.IsNullOrWhiteSpace(response.RespondentEmail))
        {
            return response.RespondentEmail;
        }

        if (!string.IsNullOrWhiteSpace(response.ContactName))
        {
            return response.ContactName;
        }

        return !string.IsNullOrWhiteSpace(response.ContactEmail)
            ? response.ContactEmail
            : "Resposta anônima";
    }

    internal static string? AuthorDetail(NpsResponseView response)
    {
        if (!string.IsNullOrWhiteSpace(response.RespondentName) && !string.IsNullOrWhiteSpace(response.RespondentEmail))
        {
            return response.RespondentEmail;
        }

        if (string.IsNullOrWhiteSpace(response.RespondentName) &&
            string.IsNullOrWhiteSpace(response.RespondentEmail) &&
            !string.IsNullOrWhiteSpace(response.ContactName) &&
            !string.IsNullOrWhiteSpace(response.ContactEmail))
        {
            return response.ContactEmail;
        }

        return null;
    }

    internal static string AspectAverage(NpsResponseView response)
    {
        var values = new[] { response.Quality, response.Schedule, response.Communication, response.BusinessValue }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();
        return values.Length == 0
            ? "—"
            : values.Average().ToString("0.0", OperationCulture);
    }
}
