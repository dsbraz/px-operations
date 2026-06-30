using PxOperations.Api.Features.Nps.Contracts;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Nps;

namespace PxOperations.Api.Features.Nps;

public static class NpsMappings
{
    public static NpsDashboardResponse ToResponse(NpsDashboardView view)
        => new(view.TotalProjects, view.OverdueProjects, view.ActiveDispatches, view.TotalResponses, view.OfficialNps, view.AverageScore, view.Detractors, view.Passives, view.Promoters);

    public static NpsProjectResponse ToResponse(NpsProjectView view)
        => new(view.Id, view.Name, view.Client, view.Dc, view.DeliveryManager, view.ContactsCount, view.ActiveDispatches, view.LinkTargetsCount, view.AnsweredLinkTargetsCount, view.ResponsesCount, view.LastResponseAt, view.LastNps, view.IsOverdue);

    public static NpsProjectDetailResponse ToResponse(NpsProjectDetailView view)
        => new(ToResponse(view.Project), view.Contacts.Select(ToResponse).ToList(), view.Dispatches.Select(ToResponse).ToList(), view.RecentResponses.Select(ToResponse).ToList());

    public static NpsContactResponse ToResponse(NpsContactView view)
        => new(view.Id, view.ProjectId, view.Name, view.Email, view.Role, view.IsArchived, view.CreatedAt, view.ArchivedAt);

    public static NpsDispatchResponse ToResponse(NpsDispatchView view)
        => new(view.Id, view.ProjectId, view.ProjectName, view.PeriodStart, view.PeriodEnd, view.Format, view.Language, view.Status, view.CreatedBy, view.CreatedAt, view.ClosedAt, view.TargetsCount, view.ResponsesCount);

    public static NpsDispatchDetailResponse ToResponse(NpsDispatchDetailView view)
        => new(ToResponse(view.Dispatch), view.Targets.Select(ToResponse).ToList());

    public static NpsDispatchTargetResponse ToResponse(NpsDispatchTargetView view)
        => new(view.Id, view.DispatchId, view.ContactId, view.ContactName, view.ContactEmail, view.Token, view.IsGeneric, view.ResponsesCount);

    public static NpsSurveyResponse ToResponse(NpsResponseView view)
        => new(view.Id, view.ProjectId, view.ProjectName, view.DispatchId, view.TargetId, view.ContactId, view.ContactName, view.ContactEmail, view.Score, view.Classification, view.Scope, view.Schedule, view.Quality, view.Communication, view.Tags, view.Comment, view.RespondentName, view.RespondentEmail, view.SubmittedAt);

    public static NpsPublicSurveyResponse ToResponse(NpsPublicSurveyView view)
        => new(view.Token, view.ProjectId, view.ProjectName, view.DispatchId, view.PeriodStart, view.PeriodEnd, view.Format, view.Language, view.AlreadyAnswered);

    public static NpsFormFormat ParseFormFormat(string value) => value.Trim().ToLowerInvariant() switch
    {
        "complete" or "completo" => NpsFormFormat.Complete,
        "simplified" or "simplificado" => NpsFormFormat.Simplified,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid NPS form format.")
    };

    public static NpsLanguage ParseLanguage(string value) => value.Trim().ToLowerInvariant() switch
    {
        "english" or "ingles" or "inglês" or "en" => NpsLanguage.English,
        "spanish" or "espanhol" or "es" => NpsLanguage.Spanish,
        "portuguese" or "portugues" or "português" or "pt" => NpsLanguage.Portuguese,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid NPS language.")
    };

    public static NpsClassification? ParseClassificationOrNull(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        null or "" => null,
        "detractor" or "detrator" => NpsClassification.Detractor,
        "passive" or "neutro" => NpsClassification.Passive,
        "promoter" or "promotor" => NpsClassification.Promoter,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid NPS classification.")
    };
}
