using PxOperations.Application.Abstractions;
using PxOperations.Domain.Projects;

namespace PxOperations.Application.Features.Projects.UseCases;

public sealed record CreateProjectCommand(
    DeliveryCenter Dc,
    ProjectStatus Status,
    string Name,
    string? Client,
    ProjectType Type,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? DeliveryManager,
    RenewalStatus Renewal,
    string? RenewalObservation);

public sealed class CreateProjectUseCase(
    IProjectRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<Project> ExecuteAsync(
        CreateProjectCommand command, CancellationToken ct)
    {
        var project = Project.Create(
            command.Dc,
            command.Status,
            command.Name,
            command.Client,
            command.Type,
            command.StartDate,
            command.EndDate,
            command.DeliveryManager,
            command.Renewal,
            command.RenewalObservation);

        repository.Add(project);
        await unitOfWork.SaveChangesAsync(ct);
        return project;
    }
}
