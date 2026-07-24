namespace PxOperations.BlazorWasm.Features.Projects.Preview;

public static class ProjectsPreviewQueryCodec
{
    private const string Route = "/preview/projects";

    private static readonly HashSet<string> DeliveryCenters =
        new(["DC1", "DC2", "DC3", "DC4", "DC5", "DC6"], StringComparer.Ordinal);

    private static readonly HashSet<string> Statuses =
        new(["Em andamento", "Programado", "Encerrado"], StringComparer.Ordinal);

    private static readonly HashSet<string> Types =
        new(["Squad", "Escopo Fechado", "Alocação"], StringComparer.Ordinal);

    private static readonly HashSet<string> Renewals =
        new(["Aprovada", "Em andamento", "Pendente", "None"], StringComparer.Ordinal);

    public static ProjectsPreviewFilterState Parse(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return ProjectsPreviewFilterState.Empty;

        var values = ParseValues(query);
        return new ProjectsPreviewFilterState(
            Search: values
                .Where(pair => pair.Key == "q")
                .Select(pair => pair.Value.Trim())
                .FirstOrDefault(value => value.Length > 0) ?? string.Empty,
            DeliveryCenters: SelectOptions(values, "dc", DeliveryCenters),
            Statuses: SelectOptions(values, "status", Statuses),
            Types: SelectOptions(values, "type", Types),
            Renewals: SelectOptions(values, "renewal", Renewals));
    }

    public static string Build(ProjectsPreviewFilterState state)
    {
        var parameters = new List<string>();
        Add(parameters, "q", string.IsNullOrWhiteSpace(state.Search)
            ? []
            : [state.Search.Trim()]);
        Add(parameters, "dc", Normalize(state.DeliveryCenters, DeliveryCenters));
        Add(parameters, "status", Normalize(state.Statuses, Statuses));
        Add(parameters, "type", Normalize(state.Types, Types));
        Add(parameters, "renewal", Normalize(state.Renewals, Renewals));

        return parameters.Count == 0
            ? Route
            : $"{Route}?{string.Join("&", parameters)}";
    }

    private static IReadOnlyList<KeyValuePair<string, string>> ParseValues(string query)
    {
        var result = new List<KeyValuePair<string, string>>();
        foreach (var segment in query.TrimStart('?').Split(
                     '&',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = segment.IndexOf('=');
            var rawKey = separator < 0 ? segment : segment[..separator];
            var rawValue = separator < 0 ? string.Empty : segment[(separator + 1)..];

            try
            {
                result.Add(new KeyValuePair<string, string>(
                    Decode(rawKey),
                    Decode(rawValue)));
            }
            catch (UriFormatException)
            {
                // Malformed values are ignored instead of breaking navigation.
            }
        }

        return result;
    }

    private static string Decode(string value) =>
        Uri.UnescapeDataString(value.Replace("+", " ", StringComparison.Ordinal));

    private static IReadOnlyList<string> SelectOptions(
        IReadOnlyList<KeyValuePair<string, string>> values,
        string key,
        HashSet<string> allowed) =>
        Normalize(
            values.Where(pair => pair.Key == key).Select(pair => pair.Value),
            allowed);

    private static IReadOnlyList<string> Normalize(
        IEnumerable<string> values,
        HashSet<string> allowed) =>
        values
            .Where(allowed.Contains)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static void Add(
        ICollection<string> parameters,
        string key,
        IEnumerable<string> values)
    {
        foreach (var value in values)
            parameters.Add($"{key}={Uri.EscapeDataString(value)}");
    }
}
