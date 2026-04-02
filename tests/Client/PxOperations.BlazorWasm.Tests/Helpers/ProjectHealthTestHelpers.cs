using System.Net;
using System.Net.Http;
using System.Text;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Tests.Helpers;

internal static class ProjectHealthTestHelpers
{
    internal static ProjectHealthResponse MakeProjectHealth(
        int id = 1,
        int projectId = 1,
        string projectName = "Projeto Teste",
        string? projectClient = "Cliente A",
        string projectDc = "DC1",
        string? projectDeliveryManager = "DM X",
        string? subProject = null,
        string week = "2026-03-30",
        string reporterEmail = "joao@brq.com",
        int practicesCount = 3,
        string scope = "Verde",
        string schedule = "Verde",
        string quality = "Verde",
        string satisfaction = "Verde",
        int score = 10,
        bool expansionOpportunity = false,
        string? expansionComment = null,
        bool actionPlanNeeded = false,
        string highlights = "Tudo certo.") => new ProjectHealthResponse
        {
            Id = id,
            ProjectId = projectId,
            ProjectName = projectName,
            ProjectClient = projectClient,
            ProjectDc = projectDc,
            ProjectDeliveryManager = projectDeliveryManager,
            SubProject = subProject,
            Week = week,
            ReporterEmail = reporterEmail,
            PracticesCount = practicesCount,
            Scope = scope,
            Schedule = schedule,
            Quality = quality,
            Satisfaction = satisfaction,
            Score = score,
            ExpansionOpportunity = expansionOpportunity,
            ExpansionComment = expansionComment,
            ActionPlanNeeded = actionPlanNeeded,
            Highlights = highlights
        };

    internal static string ProjectHealthJson(ProjectHealthResponse h)
    {
        static string Str(string? v) => v is null ? "null" : $"\"{v}\"";
        static string Bool(bool v) => v ? "true" : "false";
        return $"{{\"id\":{h.Id},\"projectId\":{h.ProjectId},\"projectName\":{Str(h.ProjectName)},\"projectClient\":{Str(h.ProjectClient)},\"projectDc\":{Str(h.ProjectDc)},\"projectDeliveryManager\":{Str(h.ProjectDeliveryManager)},\"subProject\":{Str(h.SubProject)},\"week\":{Str(h.Week)},\"reporterEmail\":{Str(h.ReporterEmail)},\"practicesCount\":{h.PracticesCount},\"scope\":{Str(h.Scope)},\"schedule\":{Str(h.Schedule)},\"quality\":{Str(h.Quality)},\"satisfaction\":{Str(h.Satisfaction)},\"score\":{h.Score},\"expansionOpportunity\":{Bool(h.ExpansionOpportunity)},\"expansionComment\":{Str(h.ExpansionComment)},\"actionPlanNeeded\":{Bool(h.ActionPlanNeeded)},\"highlights\":{Str(h.Highlights)}}}";
    }

    internal static string ProjectHealthListJson(params ProjectHealthResponse[] items)
        => $"[{string.Join(",", items.Select(ProjectHealthJson))}]";

    internal static string SummaryJson(
        int totalEntries = 0,
        int totalProjects = 0,
        double avgScore = 0,
        int criticalCount = 0,
        int noResponseCount = 0)
    {
        return $"{{\"totalEntries\":{totalEntries},\"totalProjects\":{totalProjects},\"averageScore\":{avgScore},\"averageScope\":0,\"averageSchedule\":0,\"averageQuality\":0,\"averageSatisfaction\":0,\"criticalCount\":{criticalCount},\"attentionCount\":0,\"healthyCount\":0,\"noResponseCount\":{noResponseCount},\"withExpansionCount\":0,\"withActionPlanCount\":0,\"weeklyEvolution\":[]}}";
    }
}
