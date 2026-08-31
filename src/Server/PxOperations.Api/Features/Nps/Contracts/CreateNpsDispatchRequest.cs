namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record CreateNpsDispatchRequest(int ProjectId, string Format, string Language, IReadOnlyList<int>? ContactIds);
