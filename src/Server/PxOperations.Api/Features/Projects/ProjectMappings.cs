using PxOperations.Domain.Projects;

namespace PxOperations.Api.Features.Projects;

public static class ProjectMappings
{
    public static ProjectResponse ToResponse(Project project)
    {
        return new ProjectResponse(
            project.Id,
            FormatDeliveryCenter(project.Dc),
            FormatProjectStatus(project.Status),
            project.Name,
            project.Client,
            FormatProjectType(project.Type),
            project.StartDate?.ToString("yyyy-MM-dd"),
            project.EndDate?.ToString("yyyy-MM-dd"),
            project.DeliveryManager,
            FormatRenewalStatus(project.Renewal),
            project.RenewalObservation);
    }

    public static DeliveryCenter ParseDeliveryCenter(string value)
    {
        return value.ToUpperInvariant() switch
        {
            "DC1" => DeliveryCenter.Dc1,
            "DC2" => DeliveryCenter.Dc2,
            "DC3" => DeliveryCenter.Dc3,
            "DC4" => DeliveryCenter.Dc4,
            "DC5" => DeliveryCenter.Dc5,
            "DC6" => DeliveryCenter.Dc6,
            _ => throw new ArgumentException($"Invalid delivery center: {value}")
        };
    }

    public static ProjectStatus ParseProjectStatus(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "em andamento" or "in_progress" => ProjectStatus.InProgress,
            "programado" or "scheduled" => ProjectStatus.Scheduled,
            "encerrado" or "closed" => ProjectStatus.Closed,
            _ => throw new ArgumentException($"Invalid project status: {value}")
        };
    }

    public static ProjectType ParseProjectType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "squad" => ProjectType.Squad,
            "escopo fechado" or "fixed_scope" => ProjectType.FixedScope,
            "alocacao" or "alocação" or "staffing" => ProjectType.Staffing,
            _ => throw new ArgumentException($"Invalid project type: {value}")
        };
    }

    public static RenewalStatus ParseRenewalStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("none", StringComparison.OrdinalIgnoreCase))
            return RenewalStatus.None;

        return value.ToLowerInvariant() switch
        {
            "pendente" or "pending" => RenewalStatus.Pending,
            "em andamento" or "in_progress" => RenewalStatus.InProgress,
            "aprovada" or "approved" => RenewalStatus.Approved,
            _ => throw new ArgumentException($"Invalid renewal status: {value}")
        };
    }

    public static string FormatDeliveryCenter(DeliveryCenter dc) => dc switch
    {
        DeliveryCenter.Dc1 => "DC1",
        DeliveryCenter.Dc2 => "DC2",
        DeliveryCenter.Dc3 => "DC3",
        DeliveryCenter.Dc4 => "DC4",
        DeliveryCenter.Dc5 => "DC5",
        DeliveryCenter.Dc6 => "DC6",
        _ => dc.ToString()
    };

    public static string FormatProjectStatus(ProjectStatus status) => status switch
    {
        ProjectStatus.InProgress => "Em andamento",
        ProjectStatus.Scheduled => "Programado",
        ProjectStatus.Closed => "Encerrado",
        _ => status.ToString()
    };

    public static string FormatProjectType(ProjectType type) => type switch
    {
        ProjectType.Squad => "Squad",
        ProjectType.FixedScope => "Escopo Fechado",
        ProjectType.Staffing => "Alocação",
        _ => type.ToString()
    };

    public static string FormatRenewalStatus(RenewalStatus renewal) => renewal switch
    {
        RenewalStatus.None => "None",
        RenewalStatus.Pending => "Pendente",
        RenewalStatus.InProgress => "Em andamento",
        RenewalStatus.Approved => "Aprovada",
        _ => renewal.ToString()
    };
}
