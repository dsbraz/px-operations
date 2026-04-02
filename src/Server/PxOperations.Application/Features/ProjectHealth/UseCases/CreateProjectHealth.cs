using PxOperations.Application.Abstractions;
using PxOperations.Application.Features.Projects;
using PxOperations.Domain.ProjectHealth;

namespace PxOperations.Application.Features.ProjectHealth.UseCases;

public sealed record CreateProjectHealthCommand(
    int ProjectId,
    string? SubProject,
    DateOnly Week,
    string ReporterEmail,
    int PracticesCount,
    RagStatus Scope,
    RagStatus Schedule,
    RagStatus Quality,
    RagStatus Satisfaction,
    bool ExpansionOpportunity,
    string? ExpansionComment,
    bool ActionPlanNeeded,
    string Highlights);

public sealed class CreateProjectHealthUseCase(
    IProjectHealthRepository repository,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Domain.ProjectHealth.ProjectHealth> ExecuteAsync(CreateProjectHealthCommand command, CancellationToken ct)
    {
        var projectExists = await projectRepository.ExistsAsync(command.ProjectId, ct);
        if (!projectExists) throw new KeyNotFoundException($"Project {command.ProjectId} was not found.");

        var projectHealth = Domain.ProjectHealth.ProjectHealth.Create(
            command.ProjectId,
            command.SubProject,
            command.Week,
            command.ReporterEmail,
            command.PracticesCount,
            command.Scope,
            command.Schedule,
            command.Quality,
            command.Satisfaction,
            command.ExpansionOpportunity,
            command.ExpansionComment,
            command.ActionPlanNeeded,
            command.Highlights);

        repository.Add(projectHealth);
        await unitOfWork.SaveChangesAsync(ct);
        return projectHealth;
    }
}
