namespace PxOperations.BlazorWasm.Features.Nps;

/// <summary>
/// Uma marcação de faceta pedida pela toolbar. A chave é o mesmo vocabulário
/// que os chips já usam para remover uma faceta ("client", "dc", "status"…),
/// então a página resolve chave e conjunto num lugar só.
/// </summary>
public readonly record struct NpsFacetToggle(string Key, string Value);
