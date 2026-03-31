namespace PxOperations.Api.Features.HealthChecks.Contracts;

public sealed record HealthCheckResponse(
    int Id,
    int ProjectId,
    string ProjectName,
    string? ProjectClient,
    string ProjectDc,
    string? ProjectDeliveryManager,
    string? SubProject,
    string Week,
    string ReporterEmail,
    int PracticesCount,
    string Scope,
    string Schedule,
    string Quality,
    string Satisfaction,
    int Score,
    bool ExpansionOpportunity,
    string? ExpansionComment,
    bool ActionPlanNeeded,
    string Highlights);
