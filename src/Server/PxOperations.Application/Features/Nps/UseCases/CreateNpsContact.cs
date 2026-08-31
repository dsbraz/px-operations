using PxOperations.Application.Abstractions;
using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed record CreateNpsContactCommand(int ProjectId, string Name, string Email, string? Role);

public sealed class CreateNpsContactUseCase(
    INpsRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<int> ExecuteAsync(CreateNpsContactCommand command, CancellationToken ct)
    {
        if (!await repository.ProjectExistsAsync(command.ProjectId, ct))
        {
            throw new ResourceNotFoundException("Project was not found.");
        }

        var contact = Contact.Create(
            command.ProjectId,
            command.Name,
            command.Email,
            command.Role,
            timeProvider.GetUtcNow());
        repository.AddContact(contact);
        await unitOfWork.SaveChangesAsync(ct);
        return contact.Id;
    }
}
