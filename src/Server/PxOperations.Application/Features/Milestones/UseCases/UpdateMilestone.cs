using PxOperations.Application.Abstractions;
using PxOperations.Application.Features.Projects;
using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Milestones;

namespace PxOperations.Application.Features.Milestones.UseCases;

public sealed record UpdateMilestoneCommand(
    Optional<int> ProjectId = default,
    Optional<MilestoneType> Type = default,
    Optional<string> Title = default,
    Optional<DateOnly> Date = default,
    Optional<TimeOnly?> Time = default,
    Optional<string?> Notes = default);

public sealed class UpdateMilestoneUseCase(
    IMilestoneRepository repository,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Milestone?> ExecuteAsync(int id, UpdateMilestoneCommand command, CancellationToken ct)
    {
        var milestone = await repository.GetByIdAsync(id, ct);
        if (milestone is null) return null;

        var projectId = command.ProjectId.Resolve(milestone.ProjectId);
        if (projectId != milestone.ProjectId)
        {
            var projectExists = await projectRepository.ExistsAsync(projectId, ct);
            if (!projectExists) throw new KeyNotFoundException($"Project {projectId} was not found.");
        }

        milestone.Update(
            projectId,
            command.Type.Resolve(milestone.Type),
            command.Title.Resolve(milestone.Title),
            command.Date.Resolve(milestone.Date),
            command.Time.Resolve(milestone.Time),
            command.Notes.Resolve(milestone.Notes));

        await unitOfWork.SaveChangesAsync(ct);
        return milestone;
    }
}
