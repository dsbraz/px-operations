using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Application.Abstractions;
using PxOperations.Application.Diagnostics;
using PxOperations.Application.Projects;
using PxOperations.Application.Projects.UseCases;
using PxOperations.Infrastructure.Diagnostics;
using PxOperations.Infrastructure.Persistence;
using PxOperations.Infrastructure.Projects;

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

        services.AddScoped<CreateProjectUseCase>();
        services.AddScoped<UpdateProjectUseCase>();
        services.AddScoped<DeleteProjectUseCase>();
        services.AddScoped<GetProjectUseCase>();
        services.AddScoped<ListProjectsUseCase>();

        services.AddScoped<IReadinessService, DatabaseReadinessService>();

        return services;
    }
}
