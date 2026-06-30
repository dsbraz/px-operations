namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class GetNpsPublicSurveyUseCase(INpsRepository repository)
{
    public Task<NpsPublicSurveyView?> ExecuteAsync(Guid token, CancellationToken ct)
        => repository.GetPublicSurveyAsync(token, ct);
}
