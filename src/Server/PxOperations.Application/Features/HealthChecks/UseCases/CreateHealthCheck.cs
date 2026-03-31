using PxOperations.Application.Abstractions;
using PxOperations.Application.Features.Projects;
using PxOperations.Domain.HealthChecks;

namespace PxOperations.Application.Features.HealthChecks.UseCases;

public sealed record CreateHealthCheckCommand(
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

public sealed class CreateHealthCheckUseCase(
    IHealthCheckRepository repository,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<HealthCheck> ExecuteAsync(CreateHealthCheckCommand command, CancellationToken ct)
    {
        var projectExists = await projectRepository.ExistsAsync(command.ProjectId, ct);
        if (!projectExists) throw new KeyNotFoundException($"Project {command.ProjectId} was not found.");

        var healthCheck = HealthCheck.Create(
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

        repository.Add(healthCheck);
        await unitOfWork.SaveChangesAsync(ct);
        return healthCheck;
    }
}
