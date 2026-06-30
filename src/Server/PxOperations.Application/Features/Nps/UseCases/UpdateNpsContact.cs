using PxOperations.Application.Abstractions;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed record UpdateNpsContactCommand(string Name, string Email, string? Role);

public sealed class UpdateNpsContactUseCase(INpsRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<NpsContactView?> ExecuteAsync(int id, UpdateNpsContactCommand command, CancellationToken ct)
    {
        var contact = await repository.GetContactAsync(id, ct);
        if (contact is null)
        {
            return null;
        }

        contact.Update(command.Name, command.Email, command.Role);
        await unitOfWork.SaveChangesAsync(ct);

        return (await repository.ListContactsAsync(contact.ProjectId, includeArchived: true, ct)).Single(c => c.Id == contact.Id);
    }
}
