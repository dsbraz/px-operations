using Microsoft.Extensions.DependencyInjection;
using PxOperations.Ui.Theming;

namespace PxOperations.Ui;

public static class PxOperationsUiServiceCollectionExtensions
{
    public static IServiceCollection AddPxOperationsUi(this IServiceCollection services)
    {
        services.AddScoped<IThemeService, ThemeService>();
        return services;
    }
}
