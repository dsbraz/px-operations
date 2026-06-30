using PxOperations.Application.Abstractions;
using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed record CreateNpsContactCommand(string Name, string Email, string? Role);

public sealed class CreateNpsContactUseCase(INpsRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<NpsContactView> ExecuteAsync(int projectId, CreateNpsContactCommand command, CancellationToken ct)
    {
        if (!await repository.ProjectExistsAsync(projectId, ct))
        {
            throw new KeyNotFoundException("Project not found.");
        }

        var contact = Contact.Create(projectId, command.Name, command.Email, command.Role, DateTimeOffset.UtcNow);
        repository.AddContact(contact);
        await unitOfWork.SaveChangesAsync(ct);

        return (await repository.ListContactsAsync(projectId, includeArchived: true, ct)).Single(c => c.Id == contact.Id);
    }
}
