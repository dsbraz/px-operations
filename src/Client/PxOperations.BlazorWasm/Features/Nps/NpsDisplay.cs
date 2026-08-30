using System.Globalization;

namespace PxOperations.BlazorWasm.Features.Nps;

/// <summary>
/// Formatações que mais de uma tela do NPS usa. Fica fora das páginas porque
/// os indicadores, a tabela de resultados e o detalhe da coleta precisam da
/// mesma regra de exibição — e o travessão de "sem valor" é uma delas.
/// </summary>
internal static class NpsDisplay
{
    internal static string Metric(double? value)
        => value?.ToString("0.0", CultureInfo.CurrentCulture) ?? "—";

    internal static string Count(int? value)
        => value?.ToString(CultureInfo.CurrentCulture) ?? "—";
}
