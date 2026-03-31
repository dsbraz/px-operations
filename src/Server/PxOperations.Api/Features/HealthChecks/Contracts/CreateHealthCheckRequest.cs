namespace PxOperations.Api.Features.HealthChecks.Contracts;

public sealed record CreateHealthCheckRequest(
    int ProjectId,
    string? SubProject,
    string Week,
    string ReporterEmail,
    int PracticesCount,
    string Scope,
    string Schedule,
    string Quality,
    string Satisfaction,
    bool ExpansionOpportunity,
    string? ExpansionComment,
    bool ActionPlanNeeded,
    string Highlights);
