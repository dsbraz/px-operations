using PxOperations.Application.Abstractions;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class ArchiveNpsContactUseCase(
    INpsRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<int> ExecuteAsync(int id, CancellationToken ct)
    {
        var contact = await repository.GetContactAsync(id, ct)
            ?? throw new ResourceNotFoundException("NPS contact was not found.");

        contact.Archive(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
        return contact.Id;
    }
}
