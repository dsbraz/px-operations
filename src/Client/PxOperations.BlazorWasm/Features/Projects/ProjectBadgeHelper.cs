using System.Globalization;
using Microsoft.AspNetCore.Components;
using PxOperations.Ui.Components.DataDisplay;

namespace PxOperations.BlazorWasm.Features.Projects;

internal static class ProjectBadgeHelper
{
    public static BrqStatusTone GetStatusTone(string status) => status switch
    {
        "Em andamento" => BrqStatusTone.Info,
        "Programado" => BrqStatusTone.Neutral,
        "Encerrado" => BrqStatusTone.Neutral,
        _ => BrqStatusTone.Neutral
    };

    public static BrqStatusTone GetRenewalTone(string renewal) => renewal switch
    {
        "Aprovada" => BrqStatusTone.Positive,
        "Em andamento" => BrqStatusTone.Info,
        "Pendente" => BrqStatusTone.Warning,
        _ => BrqStatusTone.Neutral
    };

    public static string GetRenewalLabel(string renewal) =>
        renewal == "None" ? "Sem renovação" : renewal;

    public static BrqTagTone GetRenewalTagTone(string renewal) => renewal switch
    {
        "Aprovada" => BrqTagTone.Green,
        "Em andamento" => BrqTagTone.Blue,
        "Pendente" => BrqTagTone.Orange,
        _ => BrqTagTone.Gray
    };

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

    public static string GetRemainingDaysLabel(string? endDate)
    {
        if (!DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
            return "Sem prazo";

        var days = (end.Date - DateTime.Today).Days;
        return days < 0 ? $"{Math.Abs(days)}d atrás" : $"{days}d";
    }

    public static BrqStatusTone GetRemainingDaysTone(string? endDate)
    {
        if (!DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
            return BrqStatusTone.Neutral;

        var days = (end.Date - DateTime.Today).Days;
        if (days < 0) return BrqStatusTone.Danger;
        if (days <= 60) return BrqStatusTone.Warning;
        return BrqStatusTone.Positive;
    }

    public static BrqTagTone GetRemainingDaysTagTone(string? endDate)
    {
        if (!DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
            return BrqTagTone.Gray;

        var days = (end.Date - DateTime.Today).Days;
        if (days <= 60) return BrqTagTone.Orange;
        return BrqTagTone.Green;
    }
}
