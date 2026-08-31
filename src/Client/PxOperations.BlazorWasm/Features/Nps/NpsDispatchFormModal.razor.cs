using System.Globalization;
using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsDispatchFormModal : ComponentBase
{
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter] public IReadOnlyList<NpsProjectView> Projects { get; set; } = [];

    [Parameter] public int ProjectId { get; set; }
    [Parameter] public EventCallback<int> ProjectIdChanged { get; set; }
    [Parameter] public string Format { get; set; } = "complete";
    [Parameter] public EventCallback<string> FormatChanged { get; set; }
    [Parameter] public string Language { get; set; } = "pt";
    [Parameter] public EventCallback<string> LanguageChanged { get; set; }

    /// <summary>Nulo até o disparo existir; a partir daí o diálogo vira o passo 2.</summary>
    [Parameter] public NpsDispatchDetailView? Created { get; set; }

    [Parameter] public string LinkUrl { get; set; } = string.Empty;
    [Parameter] public string SuggestedMessage { get; set; } = string.Empty;
    /// <summary>
    /// Trava o envio enquanto o POST está em voo: dois cliques criariam dois
    /// disparos do mesmo formato e o segundo bateria no índice único.
    /// </summary>
    [Parameter] public bool IsSubmitting { get; set; }

    [Parameter] public string? ActionError { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback OnSubmit { get; set; }
    [Parameter] public EventCallback<string> OnCopy { get; set; }

    private Task ProjectChanged(ChangeEventArgs args)
        => ProjectIdChanged.InvokeAsync(
            int.TryParse(args.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0);
}
