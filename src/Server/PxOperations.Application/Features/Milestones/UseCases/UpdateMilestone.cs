using PxOperations.Application.Abstractions;
using PxOperations.Application.Features.Projects;
using PxOperations.Domain.Milestones;

namespace PxOperations.Application.Features.Milestones.UseCases;

public sealed record UpdateMilestoneCommand(
    int? ProjectId = null,
    MilestoneType? Type = null,
    string? Title = null,
    DateOnly? Date = null,
    TimeOnly? Time = null,
    string? Notes = null);

public sealed class UpdateMilestoneUseCase(
    IMilestoneRepository repository,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Milestone?> ExecuteAsync(int id, UpdateMilestoneCommand command, CancellationToken ct)
    {
        var milestone = await repository.GetByIdAsync(id, ct);
        if (milestone is null) return null;

        var projectId = command.ProjectId ?? milestone.ProjectId;
        if (projectId != milestone.ProjectId)
        {
            var projectExists = await projectRepository.ExistsAsync(projectId, ct);
            if (!projectExists) throw new KeyNotFoundException($"Project {projectId} was not found.");
        }

        milestone.Update(
            projectId,
            command.Type ?? milestone.Type,
            command.Title ?? milestone.Title,
            command.Date ?? milestone.Date,
            command.Time ?? milestone.Time,
            command.Notes ?? milestone.Notes);

        await unitOfWork.SaveChangesAsync(ct);
        return milestone;
    }
}
