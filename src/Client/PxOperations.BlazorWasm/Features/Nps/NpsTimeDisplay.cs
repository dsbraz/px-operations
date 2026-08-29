using System.Globalization;

namespace PxOperations.BlazorWasm.Features.Nps;

/// <summary>
/// A API entrega instantes em UTC (<c>DateTimeOffset</c> com offset +00:00).
/// Formatar o valor cru mostrava 21:30 de Brasília como 00:30 do dia seguinte —
/// hora e dia errados — e encurtava em um dia a validade que o respondente lê no
/// formulário público. A conversão para o fuso de quem está olhando mora aqui,
/// num lugar só, para que nenhuma tela volte a formatar o instante sem converter.
/// O atributo <c>datetime</c> das tags &lt;time&gt; continua em ISO com offset:
/// aquele é para máquina, não para leitura.
/// </summary>
internal static class NpsTimeDisplay
{
    internal static string Date(DateTimeOffset value)
        => value.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    internal static string DateAndTime(DateTimeOffset value)
        => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    internal static string DateAndTime(DateTimeOffset? value)
        => value is null ? "—" : DateAndTime(value.Value);
}
