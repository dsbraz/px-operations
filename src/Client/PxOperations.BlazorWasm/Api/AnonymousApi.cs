namespace PxOperations.BlazorWasm.Api;

/// <summary>
/// Chave dos clients que falam com o API sem token: a pesquisa NPS pública é
/// respondida por clientes da BRQ, que não têm conta no tenant.
/// </summary>
public static class AnonymousApi
{
    public const string Key = "anonymous";
}
