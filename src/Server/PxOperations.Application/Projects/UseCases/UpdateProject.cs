using PxOperations.Application.Abstractions;
using PxOperations.Domain.Projects;

namespace PxOperations.Application.Projects.UseCases;

public sealed record UpdateProjectCommand(
    DeliveryCenter? Dc = null,
    ProjectStatus? Status = null,
    string? Name = null,
    string? Client = null,
    ProjectType? Type = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    string? DeliveryManager = null,
    RenewalStatus? Renewal = null,
    string? RenewalObservation = null);

public sealed class UpdateProjectUseCase(
    IProjectRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Project?> ExecuteAsync(
        int id, UpdateProjectCommand command, CancellationToken ct)
    {
        var project = await repository.GetByIdAsync(id, ct);
        if (project is null) return null;

        project.Update(
            command.Dc ?? project.Dc,
            command.Status ?? project.Status,
            command.Name ?? project.Name,
            command.Client ?? project.Client,
            command.Type ?? project.Type,
            command.StartDate ?? project.StartDate,
            command.EndDate ?? project.EndDate,
            command.DeliveryManager ?? project.DeliveryManager,
            command.Renewal ?? project.Renewal,
            command.RenewalObservation ?? project.RenewalObservation);

        await unitOfWork.SaveChangesAsync(ct);
        return project;
    }
}
