using PxOperations.Application.Abstractions;
using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Projects;

namespace PxOperations.Application.Features.Projects.UseCases;

public sealed record UpdateProjectCommand(
    Optional<DeliveryCenter> Dc = default,
    Optional<ProjectStatus> Status = default,
    Optional<string> Name = default,
    Optional<string?> Client = default,
    Optional<ProjectType> Type = default,
    Optional<DateOnly?> StartDate = default,
    Optional<DateOnly?> EndDate = default,
    Optional<string?> DeliveryManager = default,
    Optional<RenewalStatus> Renewal = default,
    Optional<string?> RenewalObservation = default);

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
            command.Dc.Resolve(project.Dc),
            command.Status.Resolve(project.Status),
            command.Name.Resolve(project.Name),
            command.Client.Resolve(project.Client),
            command.Type.Resolve(project.Type),
            command.StartDate.Resolve(project.StartDate),
            command.EndDate.Resolve(project.EndDate),
            command.DeliveryManager.Resolve(project.DeliveryManager),
            command.Renewal.Resolve(project.Renewal),
            command.RenewalObservation.Resolve(project.RenewalObservation));

        await unitOfWork.SaveChangesAsync(ct);
        return project;
    }
}
