namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class GetNpsFilterOptionsUseCase(INpsRepository repository)
{
    public Task<NpsFilterOptionsView> ExecuteAsync(CancellationToken ct)
        => repository.GetFilterOptionsAsync(ct);
}
