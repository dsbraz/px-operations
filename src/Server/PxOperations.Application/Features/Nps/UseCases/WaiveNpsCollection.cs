using PxOperations.Application.Abstractions;
using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed record WaiveNpsCollectionCommand(int ProjectId, string Reason);

public sealed class WaiveNpsCollectionUseCase(
    INpsRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<int> ExecuteAsync(WaiveNpsCollectionCommand command, CancellationToken ct)
    {
        if (!await repository.ProjectExistsAsync(command.ProjectId, ct))
        {
            throw new ResourceNotFoundException("Project was not found.");
        }

        var collection = await repository.GetOrCreateCollectionAsync(command.ProjectId, ct);
        collection.Waive(command.Reason, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
        return collection.Id;
    }
}
