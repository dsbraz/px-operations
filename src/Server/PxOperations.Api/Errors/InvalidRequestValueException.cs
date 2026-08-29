namespace PxOperations.Api.Errors;

/// <summary>
/// Valor de entrada que o contrato não aceita — um código de enum fora da
/// lista, por exemplo. Existe separada de <see cref="ArgumentException"/>
/// porque só ela é resposta 400: uma <see cref="ArgumentException"/> qualquer
/// nasce de defeito interno (chave duplicada em um agrupamento, argumento nulo)
/// e devolvê-la como 400 escondia o defeito das métricas de erro e ainda
/// vazava a mensagem interna para o cliente.
/// </summary>
public sealed class InvalidRequestValueException(string message) : Exception(message);
