using System.Globalization;
using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Projects;

public partial class WeeklyPulse : ComponentBase
{
    [Parameter, EditorRequired] public List<ProjectResponse> Projects { get; set; } = [];

    private bool pulseOpen = true;
    private void TogglePulse() => pulseOpen = !pulseOpen;

    private static (DateTime Monday, DateTime Sunday, DateTime PrevMonday, DateTime PrevSunday) GetWeekBounds()
    {
        var today = DateTime.Today;
        var dow = (int)today.DayOfWeek;
        var daysBack = dow == 0 ? 6 : dow - 1;
        var monday = today.AddDays(-daysBack);
        var sunday = monday.AddDays(6);
        return (monday, sunday, monday.AddDays(-7), sunday.AddDays(-7));
    }

    private string WeekLabel
    {
        get
        {
            var (mon, sun, _, _) = GetWeekBounds();
            return $"{mon:dd/MM} – {sun:dd/MM}";
        }
    }

    private List<ProjectResponse> NewScheduled =>
        Projects.Where(p => p.Status == "Programado").ToList();

    private List<ProjectResponse> StartedLastWeek
    {
        get
        {
            var (_, _, prevMon, prevSun) = GetWeekBounds();
            return Projects.Where(p =>
            {
                if (p.StartDate is null) return false;
                if (!DateTime.TryParse(p.StartDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return false;
                return d.Date >= prevMon.Date && d.Date <= prevSun.Date;
            }).ToList();
        }
    }

    private List<ProjectResponse> EndedLastWeek
    {
        get
        {
            var (_, _, prevMon, prevSun) = GetWeekBounds();
            return Projects.Where(p =>
            {
                if (p.EndDate is null) return false;
                if (!DateTime.TryParse(p.EndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return false;
                return d.Date >= prevMon.Date && d.Date <= prevSun.Date;
            }).ToList();
        }
    }

    private List<ProjectResponse> RenewalApproved =>
        Projects.Where(p => p.Renewal == "Aprovada").ToList();
}
