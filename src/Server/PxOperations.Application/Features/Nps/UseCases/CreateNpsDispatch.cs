using PxOperations.Application.Abstractions;
using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed record CreateNpsDispatchCommand(
    int ProjectId,
    string Format,
    string Language,
    IReadOnlyList<int> ContactIds);

public sealed class CreateNpsDispatchUseCase(
    INpsRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<int> ExecuteAsync(CreateNpsDispatchCommand command, CancellationToken ct)
    {
        if (!await repository.ProjectExistsAsync(command.ProjectId, ct))
        {
            throw new ResourceNotFoundException("Project was not found.");
        }

        foreach (var contactId in command.ContactIds.Distinct())
        {
            if (!await repository.ContactBelongsToProjectAsync(command.ProjectId, contactId, ct))
            {
                throw new BusinessRuleValidationException("NPS contact does not belong to the project.");
            }
        }

        var collection = await repository.GetOrCreateCollectionAsync(command.ProjectId, ct);
        var contactIds = command.ContactIds.Distinct().ToArray();
        var dispatch = collection.CreateDispatch(
            NpsCodes.ParseFormat(command.Format),
            NpsCodes.ParseLanguage(command.Language),
            contactIds,
            Guid.NewGuid(),
            contactIds.Select(_ => Guid.NewGuid()).ToArray(),
            timeProvider.GetUtcNow());

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception exception) when (repository.IsDuplicateDispatchException(exception))
        {
            throw new BusinessStateConflictException(
                "An open NPS dispatch already exists for this format.", exception);
        }

        return dispatch.Id;
    }
}
