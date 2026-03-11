using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace PxOperations.BlazorWasm.Features.Projects;

internal static class ProjectBadgeHelper
{
    public static string GetStatusBadgeClass(string status) => status switch
    {
        "Em andamento" => "sb-and",
        "Programado"   => "sb-prog",
        _              => "sb-enc"
    };

    public static string GetStatusDot(string status) => status switch
    {
        "Em andamento" => "●",
        "Programado"   => "◌",
        _              => "○"
    };

    public static string GetTypeBadgeClass(string type) => type switch
    {
        "Squad"          => "tb-squad",
        "Escopo Fechado" => "tb-escopo",
        _                => "tb-aloc"
    };

    public static string GetRenewalBadgeClass(string renewal) => renewal switch
    {
        "Aprovada"     => "rb-ap",
        "Em andamento" => "rb-and",
        "Pendente"     => "rb-pend",
        _              => "rb-na"
    };

    public static string GetRenewalIcon(string renewal) => renewal switch
    {
        "Aprovada"     => "✓",
        "Em andamento" => "↻",
        "Pendente"     => "⚑",
        _              => ""
    };

    public static MarkupString FormatDate(string? date)
    {
        if (date is null) return new("<span class=\"dtbd\">—</span>");
        if (!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return new("<span class=\"dtbd\">—</span>");
        return new($"<span class=\"dval\">{d:dd/MM/yyyy}</span>");
    }

    public static MarkupString RenderRemainingDays(string? endDate)
    {
        if (endDate is null) return new("<span class=\"dpill dp-na\">—</span>");
        if (!DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
            return new("<span class=\"dpill dp-na\">—</span>");
        var days = (end.Date - DateTime.Today).Days;
        if (days < 0) return new($"<span class=\"dpill dp-c\">{Math.Abs(days)}d atrás</span>");
        if (days <= 60) return new($"<span class=\"dpill dp-w\">{days}d</span>");
        return new($"<span class=\"dpill dp-ok\">{days}d</span>");
    }
}
