using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Application.Abstractions;
using PxOperations.Application.Features.Diagnostics;
using PxOperations.Application.Features.Nps;
using PxOperations.Application.Features.Nps.UseCases;
using PxOperations.Application.Features.ProjectHealth;
using PxOperations.Application.Features.ProjectHealth.UseCases;
using PxOperations.Application.Features.Milestones;
using PxOperations.Application.Features.Milestones.UseCases;
using PxOperations.Application.Features.Projects;
using PxOperations.Application.Features.Projects.UseCases;
using PxOperations.Infrastructure.Features.Diagnostics;
using PxOperations.Infrastructure.Features.Nps;
using PxOperations.Infrastructure.Features.ProjectHealth;
using PxOperations.Infrastructure.Features.Milestones;
using PxOperations.Infrastructure.Persistence;
using PxOperations.Infrastructure.Features.Projects;

namespace PxOperations.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"));
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IMilestoneRepository, MilestoneRepository>();

        services.AddScoped<CreateProjectUseCase>();
        services.AddScoped<UpdateProjectUseCase>();
        services.AddScoped<DeleteProjectUseCase>();
        services.AddScoped<GetProjectUseCase>();
        services.AddScoped<ListProjectsUseCase>();
        services.AddScoped<CreateMilestoneUseCase>();
        services.AddScoped<UpdateMilestoneUseCase>();
        services.AddScoped<DeleteMilestoneUseCase>();
        services.AddScoped<GetMilestoneUseCase>();
        services.AddScoped<ListMilestonesUseCase>();

        services.AddScoped<IProjectHealthRepository, ProjectHealthRepository>();
        services.AddScoped<CreateProjectHealthUseCase>();
        services.AddScoped<UpdateProjectHealthUseCase>();
        services.AddScoped<DeleteProjectHealthUseCase>();
        services.AddScoped<GetProjectHealthUseCase>();
        services.AddScoped<ListProjectHealthUseCase>();
        services.AddScoped<GetProjectHealthSummaryUseCase>();

        services.AddScoped<INpsRepository, NpsRepository>();
        services.AddScoped<GetNpsDashboardUseCase>();
        services.AddScoped<ListNpsProjectsUseCase>();
        services.AddScoped<GetNpsProjectUseCase>();
        services.AddScoped<ListNpsContactsUseCase>();
        services.AddScoped<CreateNpsContactUseCase>();
        services.AddScoped<UpdateNpsContactUseCase>();
        services.AddScoped<DeleteNpsContactUseCase>();
        services.AddScoped<ListNpsDispatchesUseCase>();
        services.AddScoped<GetNpsDispatchUseCase>();
        services.AddScoped<CreateNpsDispatchUseCase>();
        services.AddScoped<CloseNpsDispatchUseCase>();
        services.AddScoped<ListNpsResponsesUseCase>();
        services.AddScoped<GetNpsPublicSurveyUseCase>();
        services.AddScoped<SubmitNpsPublicResponseUseCase>();

        services.AddScoped<IReadinessService, DatabaseReadinessService>();

        return services;
    }
}
