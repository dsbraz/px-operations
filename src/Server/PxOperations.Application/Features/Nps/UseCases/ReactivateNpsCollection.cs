using PxOperations.Application.Abstractions;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class ReactivateNpsCollectionUseCase(INpsRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<int> ExecuteAsync(int projectId, CancellationToken ct)
    {
        var collection = await repository.GetCollectionAsync(projectId, ct)
            ?? throw new ResourceNotFoundException("NPS collection waiver was not found.");

        collection.Reactivate();
        await unitOfWork.SaveChangesAsync(ct);
        return collection.Id;
    }
}
