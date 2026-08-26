using PxOperations.Application.Abstractions;

namespace PxOperations.Application.Features.Nps.UseCases;

/// <summary>
/// F6: a volta atrás é parte do fluxo, não um detalhe — o card dispensado traz
/// a ação de reativar, e reativar devolve o projeto à coluna que a regra indicar,
/// sem perder histórico de respostas.
/// </summary>
public sealed class ReactivateNpsCollectionUseCase(INpsRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<NpsProjectView?> ExecuteAsync(int projectId, CancellationToken ct)
    {
        if (!await repository.ProjectExistsAsync(projectId, ct))
        {
            return null;
        }

        var waiver = await repository.GetActiveWaiverAsync(projectId, ct);
        if (waiver is not null)
        {
            waiver.Reactivate(DateTimeOffset.UtcNow);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return await DismissNpsCollectionUseCase.ReadBackAsync(repository, projectId, ct);
    }
}
