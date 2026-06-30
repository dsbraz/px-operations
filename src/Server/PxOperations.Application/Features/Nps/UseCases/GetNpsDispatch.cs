namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class GetNpsDispatchUseCase(INpsRepository repository)
{
    public Task<NpsDispatchDetailView?> ExecuteAsync(int id, CancellationToken ct)
        => repository.GetDispatchAsync(id, ct);
}
