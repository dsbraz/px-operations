using PxOperations.Application.Abstractions;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class DeleteNpsContactUseCase(INpsRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(int id, CancellationToken ct)
    {
        var contact = await repository.GetContactAsync(id, ct);
        if (contact is null)
        {
            return false;
        }

        contact.Archive(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
