using System.Globalization;

namespace PxOperations.BlazorWasm.Features.ProjectHealth;

internal static class ProjectHealthFormat
{
    // Weeks arrive from the API as ISO 8601 (yyyy-MM-dd); parse with the invariant culture and
    // render the month abbreviation in pt-BR. Falls back to the raw value if it cannot be parsed.
    internal static string FormatWeek(string week)
        => DateOnly.TryParseExact(week, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("dd MMM", CultureInfo.GetCultureInfo("pt-BR"))
            : week;

    internal static string ScoreClass(int score) => score >= 7 ? "score-hi" : score >= 4 ? "score-md" : "score-lo";
}
