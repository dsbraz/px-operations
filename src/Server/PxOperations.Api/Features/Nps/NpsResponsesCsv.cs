using System.Globalization;
using System.Text;
using PxOperations.Application.Features.Nps;

namespace PxOperations.Api.Features.Nps;

/// <summary>
/// Serializa o export de respostas. Vive fora do controller porque formatar um
/// arquivo não é rotear uma requisição — nenhum outro controller do repositório
/// carrega serialização.
/// </summary>
public static class NpsResponsesCsv
{
    public static string Build(IEnumerable<NpsResponseView> responses)
    {
        var builder = new StringBuilder();
        builder.AppendLine("id,project_id,project_name,dispatch_id,target_id,format,score,classification,quality,schedule,communication,business_value,comment,respondent_name,respondent_email,submitted_at");
        foreach (var response in responses)
        {
            builder.AppendLine(string.Join(',', new[]
            {
                response.Id.ToString(CultureInfo.InvariantCulture),
                response.ProjectId.ToString(CultureInfo.InvariantCulture),
                Csv(response.ProjectName),
                response.DispatchId.ToString(CultureInfo.InvariantCulture),
                response.TargetId.ToString(CultureInfo.InvariantCulture),
                response.Format,
                response.Score.ToString(CultureInfo.InvariantCulture),
                response.Classification,
                response.Quality?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                response.Schedule?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                response.Communication?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                response.BusinessValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                Csv(response.Comment),
                Csv(response.RespondentName),
                Csv(response.RespondentEmail),
                response.SubmittedAt.ToString("O", CultureInfo.InvariantCulture)
            }));
        }

        return builder.ToString();
    }

    // O Excel avalia como fórmula um campo que comece com =, +, - ou @, mesmo
    // entre aspas. Comment e RespondentName chegam do formulário público, que é
    // anônimo, então um respondente poderia plantar uma fórmula que roda na
    // máquina de quem abre o export. A apóstrofe inicial desarma a avaliação e
    // some na exibição da planilha.
    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var safe = FormulaTriggers.Contains(value[0]) ? $"'{value}" : value;
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }

    private static readonly System.Buffers.SearchValues<char> FormulaTriggers =
        System.Buffers.SearchValues.Create("=+-@\t\r");
}
