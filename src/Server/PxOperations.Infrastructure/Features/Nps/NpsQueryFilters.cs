using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Projects;

namespace PxOperations.Infrastructure.Features.Nps;

/// <summary>
/// Compõe os predicados que recortam projetos e respostas a partir do filtro.
/// Separado de NpsQueries porque montar consulta e traduzir resultado em view
/// são trabalhos diferentes.
/// </summary>
internal static class NpsQueryFilters
{
    // As datas do filtro chegam sem fuso e precisam virar instantes. Ancorá-las
    // em UTC divergia do que a tela mostra: uma resposta exibida como 31/08
    // 21:30 caía fora de "até 31/08" porque, em UTC, ela é 01/09. O cliente
    // formata no mesmo deslocamento (NpsTimeDisplay), então as duas metades do
    // recurso concordam por construção. É um deslocamento fixo, não um fuso
    // completo: o Brasil não observa horário de verão desde 2019 — se voltar a
    // observar, este é o ponto a revisitar, junto com o par no cliente.
    internal static readonly TimeSpan OperationOffset = TimeSpan.FromHours(-3);

    internal static IQueryable<Project> ApplyProjectFilters(IQueryable<Project> query, NpsFilter filter)
    {
        if (filter.ProjectId.HasValue)
        {
            query = query.Where(project => project.Id == filter.ProjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = SearchPattern(filter.Search);
            query = query.Where(project =>
                EF.Functions.ILike(project.Name, pattern, SearchEscape) ||
                (project.Client != null && EF.Functions.ILike(project.Client, pattern, SearchEscape)));
        }

        if (filter.Clients.Count != 0)
        {
            query = query.Where(project => project.Client != null && filter.Clients.Contains(project.Client));
        }

        if (filter.Dcs.Count != 0)
        {
            var values = filter.Dcs.Select(NpsCodes.ParseDc).ToArray();
            query = query.Where(project => values.Contains(project.Dc));
        }

        if (filter.ProjectTypes.Count != 0)
        {
            var values = filter.ProjectTypes.Select(NpsCodes.ParseProjectType).ToArray();
            query = query.Where(project => values.Contains(project.Type));
        }

        if (filter.DeliveryManagers.Count != 0)
        {
            query = query.Where(project => project.DeliveryManager != null && filter.DeliveryManagers.Contains(project.DeliveryManager));
        }

        return query;
    }

    // % e _ são curingas do LIKE: sem escapar, buscar "100%" casava qualquer
    // nome contendo 100, e "a_b" casava "axb". O termo é digitado pelo operador,
    // então isso é ruído de busca, não brecha — mas o resultado fica errado.
    internal const string SearchEscape = "\\";

    internal static string SearchPattern(string search)
        => $"%{search.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)}%";

    internal static bool WantsProjectsWithoutResponses(IReadOnlyList<string> statuses)
        => statuses.Any(status =>
            status.Equals("pending", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("link_generated", StringComparison.OrdinalIgnoreCase));

    internal static IQueryable<SurveyResponse> ApplyResponsePeriodFilters(
        IQueryable<SurveyResponse> query,
        NpsFilter filter)
    {
        if (filter.From.HasValue)
        {
            // O Npgsql só aceita offset zero em timestamptz: ancoramos a data no
            // deslocamento da operação e convertemos o instante resultante.
            var from = new DateTimeOffset(filter.From.Value.ToDateTime(TimeOnly.MinValue), OperationOffset)
                .ToUniversalTime();
            query = query.Where(response => response.SubmittedAt >= from);
        }

        if (filter.To.HasValue)
        {
            var until = new DateTimeOffset(filter.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), OperationOffset)
                .ToUniversalTime();
            query = query.Where(response => response.SubmittedAt < until);
        }

        return query;
    }

    internal static IQueryable<Project> ApplyProjectResultStatusFilters(
        IQueryable<Project> query,
        IReadOnlyList<string> statuses,
        IQueryable<int> responseProjectIds,
        IQueryable<int> openDispatchProjectIds)
    {
        if (statuses.Count == 0)
        {
            return query;
        }

        var responded = statuses.Contains("responded", StringComparer.OrdinalIgnoreCase);
        var linkGenerated = statuses.Contains("link_generated", StringComparer.OrdinalIgnoreCase);
        var pending = statuses.Contains("pending", StringComparer.OrdinalIgnoreCase);
        return query.Where(project =>
            (responded && responseProjectIds.Contains(project.Id)) ||
            (linkGenerated && !responseProjectIds.Contains(project.Id) && openDispatchProjectIds.Contains(project.Id)) ||
            (pending && !responseProjectIds.Contains(project.Id) && !openDispatchProjectIds.Contains(project.Id)));
    }
}
