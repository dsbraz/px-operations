using PxOperations.Application.Abstractions;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed record UpdateNpsContactCommand(int Id, string Name, string Email, string? Role);

public sealed class UpdateNpsContactUseCase(INpsRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<int> ExecuteAsync(UpdateNpsContactCommand command, CancellationToken ct)
    {
        var contact = await repository.GetContactAsync(command.Id, ct)
            ?? throw new ResourceNotFoundException("NPS contact was not found.");

        contact.Update(command.Name, command.Email, command.Role);
        await unitOfWork.SaveChangesAsync(ct);
        return contact.Id;
    }
}
