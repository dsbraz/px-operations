using PxOperations.Application.Abstractions;
using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed record DismissNpsCollectionCommand(string Reason);

/// <summary>
/// F6: dispensar a coleta de um projeto, com motivo. O projeto sai do quadro e
/// da conta de vencidos — é a única forma de tirar ruído da tela (D9).
/// </summary>
public sealed class DismissNpsCollectionUseCase(INpsRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<NpsProjectView?> ExecuteAsync(int projectId, DismissNpsCollectionCommand command, CancellationToken ct)
    {
        if (!await repository.ProjectExistsAsync(projectId, ct))
        {
            return null;
        }

        // Dispensar duas vezes não cria duas dispensas: a segunda é ruído, não
        // fato novo. O índice parcial no banco garante o mesmo sob concorrência.
        if (await repository.GetActiveWaiverAsync(projectId, ct) is null)
        {
            repository.AddWaiver(CollectionWaiver.Dismiss(projectId, command.Reason, DateTimeOffset.UtcNow));

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (repository.IsDuplicateWaiverException(ex))
            {
                // Duplo clique ou requisição repetida: o resultado desejado já
                // existe. Perder a corrida aqui é sucesso, não erro.
            }
        }

        return await ReadBackAsync(repository, projectId, ct);
    }

    /// <summary>
    /// IncludeDismissed obrigatório: acabamos de dispensar, então o projeto não
    /// aparece mais na listagem padrão — e é justamente ele que o cliente espera
    /// de volta para atualizar o card.
    /// </summary>
    internal static async Task<NpsProjectView?> ReadBackAsync(INpsRepository repository, int projectId, CancellationToken ct)
        => (await repository.ListProjectsAsync(
                NpsFilter.ForProject(projectId) with { IncludeDismissed = true }, ct))
            .FirstOrDefault();
}
