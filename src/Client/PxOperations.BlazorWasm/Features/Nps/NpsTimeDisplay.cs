using System.Globalization;

namespace PxOperations.BlazorWasm.Features.Nps;

/// <summary>
/// A API entrega instantes em UTC (<c>DateTimeOffset</c> com offset +00:00).
/// Formatar o valor cru mostrava 21:30 de Brasília como 00:30 do dia seguinte —
/// hora e dia errados — e encurtava em um dia a validade que o respondente lê no
/// formulário público.
///
/// A conversão usa o deslocamento da operação, não o fuso do navegador: os
/// limites de período do filtro são ancorados no mesmo deslocamento no servidor
/// (<c>NpsQueries.OperationOffset</c>), então filtrar "até 31/08" e ler "31/08
/// 21:30" concordam por construção. Com o fuso do navegador, quem abrisse o
/// painel de outro país veria uma data que o filtro não reconhece.
///
/// É um deslocamento fixo, não um fuso completo: o Brasil não observa horário de
/// verão desde 2019. Se voltar a observar, este é o ponto a revisitar — junto
/// com o par no servidor.
///
/// O atributo <c>datetime</c> das tags &lt;time&gt; continua em ISO com offset:
/// aquele é para máquina, não para leitura.
/// </summary>
internal static class NpsTimeDisplay
{
    private static readonly TimeSpan OperationOffset = TimeSpan.FromHours(-3);

    internal static string Date(DateTimeOffset value)
        => value.ToOffset(OperationOffset).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    internal static string DateAndTime(DateTimeOffset value)
        => value.ToOffset(OperationOffset).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    internal static string DateAndTime(DateTimeOffset? value)
        => value is null ? "—" : DateAndTime(value.Value);
}
