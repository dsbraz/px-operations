namespace PxOperations.Api.Features.ProjectHealth.Contracts;

public sealed record CreateProjectHealthRequest(
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
