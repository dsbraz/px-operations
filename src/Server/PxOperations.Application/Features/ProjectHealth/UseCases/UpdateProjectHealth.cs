using PxOperations.Application.Abstractions;
using PxOperations.Application.Features.Projects;
using PxOperations.Domain.Abstractions;
using PxOperations.Domain.ProjectHealth;

namespace PxOperations.Application.Features.ProjectHealth.UseCases;

public sealed record UpdateProjectHealthCommand(
    Optional<int> ProjectId = default,
    Optional<string?> SubProject = default,
    Optional<DateOnly> Week = default,
    Optional<string> ReporterEmail = default,
    Optional<int> PracticesCount = default,
    Optional<RagStatus> Scope = default,
    Optional<RagStatus> Schedule = default,
    Optional<RagStatus> Quality = default,
    Optional<RagStatus> Satisfaction = default,
    Optional<bool> ExpansionOpportunity = default,
    Optional<string?> ExpansionComment = default,
    Optional<bool> ActionPlanNeeded = default,
    Optional<string> Highlights = default);

public sealed class UpdateProjectHealthUseCase(
    IProjectHealthRepository repository,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Domain.ProjectHealth.ProjectHealth?> ExecuteAsync(int id, UpdateProjectHealthCommand command, CancellationToken ct)
    {
        var projectHealth = await repository.GetByIdAsync(id, ct);
        if (projectHealth is null) return null;

        var projectId = command.ProjectId.Resolve(projectHealth.ProjectId);
        if (projectId != projectHealth.ProjectId)
        {
            var projectExists = await projectRepository.ExistsAsync(projectId, ct);
            if (!projectExists) throw new KeyNotFoundException($"Project {projectId} was not found.");
        }

        projectHealth.Update(
            projectId,
            command.SubProject.Resolve(projectHealth.SubProject),
            command.Week.Resolve(projectHealth.Week),
            command.ReporterEmail.Resolve(projectHealth.ReporterEmail),
            command.PracticesCount.Resolve(projectHealth.PracticesCount),
            command.Scope.Resolve(projectHealth.Scope),
            command.Schedule.Resolve(projectHealth.Schedule),
            command.Quality.Resolve(projectHealth.Quality),
            command.Satisfaction.Resolve(projectHealth.Satisfaction),
            command.ExpansionOpportunity.Resolve(projectHealth.ExpansionOpportunity),
            command.ExpansionComment.Resolve(projectHealth.ExpansionComment),
            command.ActionPlanNeeded.Resolve(projectHealth.ActionPlanNeeded),
            command.Highlights.Resolve(projectHealth.Highlights));

        await unitOfWork.SaveChangesAsync(ct);
        return projectHealth;
    }
}
