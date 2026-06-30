using PxOperations.Application.Abstractions;
using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed record CreateNpsDispatchCommand(
    int ProjectId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    NpsFormFormat Format,
    NpsLanguage Language,
    string CreatedBy,
    IReadOnlyCollection<int> ContactIds,
    bool CreateGenericToken);

public sealed class CreateNpsDispatchUseCase(INpsRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<NpsDispatchDetailView> ExecuteAsync(CreateNpsDispatchCommand command, CancellationToken ct)
    {
        if (!await repository.ProjectExistsAsync(command.ProjectId, ct))
        {
            throw new KeyNotFoundException("Project not found.");
        }

        foreach (var contactId in command.ContactIds.Distinct())
        {
            if (!await repository.ContactBelongsToProjectAsync(command.ProjectId, contactId, ct))
            {
                throw new InvalidOperationException($"Contact {contactId} does not belong to project {command.ProjectId}.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var dispatch = Dispatch.Create(
            command.ProjectId,
            command.PeriodStart,
            command.PeriodEnd,
            command.Format,
            command.Language,
            command.CreatedBy,
            now);

        repository.AddDispatch(dispatch);
        await unitOfWork.SaveChangesAsync(ct);

        foreach (var contactId in command.ContactIds.Distinct())
        {
            repository.AddDispatchTarget(DispatchTarget.CreateContact(command.ProjectId, dispatch.Id, contactId, Guid.NewGuid(), now));
        }

        if (command.CreateGenericToken || command.ContactIds.Count == 0)
        {
            repository.AddDispatchTarget(DispatchTarget.CreateGeneric(command.ProjectId, dispatch.Id, Guid.NewGuid(), now));
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await repository.GetDispatchAsync(dispatch.Id, ct))!;
    }
}
