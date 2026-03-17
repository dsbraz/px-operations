using PxOperations.Application.Abstractions;
using PxOperations.Application.Features.Projects;
using PxOperations.Domain.Milestones;

namespace PxOperations.Application.Features.Milestones.UseCases;

public sealed record CreateMilestoneCommand(
    int ProjectId,
    MilestoneType Type,
    string Title,
    DateOnly Date,
    TimeOnly? Time,
    string? Notes);

public sealed class CreateMilestoneUseCase(
    IMilestoneRepository repository,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Milestone> ExecuteAsync(CreateMilestoneCommand command, CancellationToken ct)
    {
        var projectExists = await projectRepository.ExistsAsync(command.ProjectId, ct);
        if (!projectExists) throw new KeyNotFoundException($"Project {command.ProjectId} was not found.");

        var milestone = Milestone.Create(
            command.ProjectId,
            command.Type,
            command.Title,
            command.Date,
            command.Time,
            command.Notes);

        repository.Add(milestone);
        await unitOfWork.SaveChangesAsync(ct);
        return milestone;
    }
}
