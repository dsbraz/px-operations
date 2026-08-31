using PxOperations.Ui.Components.DataDisplay;

namespace PxOperations.BlazorWasm.Features.Nps;

/// <summary>
/// Traduz o tom que o servidor manda no código do design system. O servidor é
/// quem decide a faixa; aqui só se escolhe a cor que a representa.
/// </summary>
internal static class NpsTone
{
    internal static BrqStatusTone From(string tone) => tone switch
    {
        "positive" => BrqStatusTone.Positive,
        "warning" => BrqStatusTone.Warning,
        "critical" => BrqStatusTone.Danger,
        "info" => BrqStatusTone.Info,
        _ => BrqStatusTone.Neutral
    };
}
