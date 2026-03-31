using PxOperations.Api.Features.HealthChecks.Contracts;
using PxOperations.Application.Features.HealthChecks;
using PxOperations.Domain.HealthChecks;
using PxOperations.Domain.Projects;
using ProjectMappings = PxOperations.Api.Features.Projects.ProjectMappings;

namespace PxOperations.Api.Features.HealthChecks;

public static class HealthCheckMappings
{
    public static HealthCheckResponse ToResponse(HealthCheckView view)
    {
        return new HealthCheckResponse(
            view.Id,
            view.ProjectId,
            view.ProjectName,
            view.ProjectClient,
            view.ProjectDc,
            view.ProjectDeliveryManager,
            view.SubProject,
            view.Week,
            view.ReporterEmail,
            view.PracticesCount,
            view.Scope,
            view.Schedule,
            view.Quality,
            view.Satisfaction,
            view.Score,
            view.ExpansionOpportunity,
            view.ExpansionComment,
            view.ActionPlanNeeded,
            view.Highlights);
    }

    public static HealthCheckSummaryResponse ToSummaryResponse(HealthCheckSummary summary)
    {
        return new HealthCheckSummaryResponse(
            summary.TotalEntries,
            summary.TotalProjects,
            summary.AverageScore,
            summary.AverageScope,
            summary.AverageSchedule,
            summary.AverageQuality,
            summary.AverageSatisfaction,
            summary.CriticalCount,
            summary.AttentionCount,
            summary.HealthyCount,
            summary.NoResponseCount,
            summary.WithExpansionCount,
            summary.WithActionPlanCount,
            summary.WeeklyEvolution.Select(w =>
                new WeeklyScorePointResponse(w.Week, w.AverageScore, w.EntryCount)).ToList());
    }

    public static RagStatus ParseRagStatus(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "green" or "verde" => RagStatus.Green,
            "yellow" or "amarelo" => RagStatus.Yellow,
            "red" or "vermelho" => RagStatus.Red,
            _ => throw new ArgumentException($"Invalid RAG status: {value}")
        };
    }

    public static DeliveryCenter? ParseDeliveryCenterOrNull(string? value)
        => value is null ? null : ProjectMappings.ParseDeliveryCenter(value);
}
