using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Application.Diagnostics;
using PxOperations.Infrastructure.Diagnostics;
using PxOperations.Infrastructure.Persistence;

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

        services.AddScoped<IReadinessService, DatabaseReadinessService>();

        return services;
    }
}
