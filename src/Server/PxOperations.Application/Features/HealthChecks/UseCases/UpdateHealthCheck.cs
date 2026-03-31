using PxOperations.Application.Abstractions;
using PxOperations.Application.Features.Projects;
using PxOperations.Domain.Abstractions;
using PxOperations.Domain.HealthChecks;

namespace PxOperations.Application.Features.HealthChecks.UseCases;

public sealed record UpdateHealthCheckCommand(
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

public sealed class UpdateHealthCheckUseCase(
    IHealthCheckRepository repository,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<HealthCheck?> ExecuteAsync(int id, UpdateHealthCheckCommand command, CancellationToken ct)
    {
        var healthCheck = await repository.GetByIdAsync(id, ct);
        if (healthCheck is null) return null;

        var projectId = command.ProjectId.Resolve(healthCheck.ProjectId);
        if (projectId != healthCheck.ProjectId)
        {
            var projectExists = await projectRepository.ExistsAsync(projectId, ct);
            if (!projectExists) throw new KeyNotFoundException($"Project {projectId} was not found.");
        }

        healthCheck.Update(
            projectId,
            command.SubProject.Resolve(healthCheck.SubProject),
            command.Week.Resolve(healthCheck.Week),
            command.ReporterEmail.Resolve(healthCheck.ReporterEmail),
            command.PracticesCount.Resolve(healthCheck.PracticesCount),
            command.Scope.Resolve(healthCheck.Scope),
            command.Schedule.Resolve(healthCheck.Schedule),
            command.Quality.Resolve(healthCheck.Quality),
            command.Satisfaction.Resolve(healthCheck.Satisfaction),
            command.ExpansionOpportunity.Resolve(healthCheck.ExpansionOpportunity),
            command.ExpansionComment.Resolve(healthCheck.ExpansionComment),
            command.ActionPlanNeeded.Resolve(healthCheck.ActionPlanNeeded),
            command.Highlights.Resolve(healthCheck.Highlights));

        await unitOfWork.SaveChangesAsync(ct);
        return healthCheck;
    }
}
