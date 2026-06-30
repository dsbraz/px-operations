namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class GetNpsDashboardUseCase(INpsRepository repository)
{
    public Task<NpsDashboardView> ExecuteAsync(NpsFilter filter, CancellationToken ct)
        => repository.GetDashboardAsync(filter, ct);
}
