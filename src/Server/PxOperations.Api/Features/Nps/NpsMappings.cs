using PxOperations.Api.Features.Nps.Contracts;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Projects;

namespace PxOperations.Api.Features.Nps;

public static class NpsMappings
{
    public static NpsDashboardResponse ToResponse(NpsDashboardView view)
        => new(view.TotalProjects, view.OverdueProjects, view.ActiveDispatches, view.TotalResponses, view.OfficialNps, view.AverageScore, view.Detractors, view.Passives, view.Promoters);

    public static NpsProjectResponse ToResponse(NpsProjectView view)
        => new(view.Id, view.Name, view.Client, view.Dc, view.DeliveryManager, view.ContactsCount, view.ActiveDispatches, view.LinkTargetsCount, view.AnsweredLinkTargetsCount, view.ResponsesCount, view.LastResponseAt, view.LastNps, view.IsOverdue, view.CollectionStatus, view.IsDismissed, view.DismissalReason, view.ActiveDispatchExpiresAt, view.LastDispatchClosedAt);

    public static NpsProjectDetailResponse ToResponse(NpsProjectDetailView view)
        => new(ToResponse(view.Project), view.Contacts.Select(ToResponse).ToList(), view.Dispatches.Select(ToResponse).ToList(), view.RecentResponses.Select(ToResponse).ToList());

    public static NpsContactResponse ToResponse(NpsContactView view)
        => new(view.Id, view.ProjectId, view.Name, view.Email, view.Role, view.IsArchived, view.CreatedAt, view.ArchivedAt);

    public static NpsDispatchResponse ToResponse(NpsDispatchView view)
        => new(view.Id, view.ProjectId, view.ProjectName, view.PeriodStart, view.PeriodEnd, view.Format, view.Language, view.Status, view.CreatedBy, view.CreatedAt, view.ClosedAt, view.ExpiresAt, view.IsExpired, view.TargetsCount, view.ResponsesCount);

    public static NpsDispatchDetailResponse ToResponse(NpsDispatchDetailView view)
        => new(ToResponse(view.Dispatch), view.Targets.Select(ToResponse).ToList());

    public static NpsDispatchTargetResponse ToResponse(NpsDispatchTargetView view)
        => new(view.Id, view.DispatchId, view.ContactId, view.ContactName, view.ContactEmail, view.Token, view.IsGeneric, view.ResponsesCount);

    public static NpsSurveyResponse ToResponse(NpsResponseView view)
        => new(view.Id, view.ProjectId, view.ProjectName, view.DispatchId, view.TargetId, view.ContactId, view.ContactName, view.ContactEmail, view.Score, view.Classification, view.Format, view.BusinessValue, view.Schedule, view.Quality, view.Communication, view.Tags, view.Comment, view.RespondentName, view.RespondentEmail, view.SubmittedAt);

    public static NpsPublicSurveyResponse ToResponse(NpsPublicSurveyView view)
        => new(view.Token, view.ProjectId, view.ProjectName, view.DispatchId, view.PeriodStart, view.PeriodEnd, view.Format, view.Language, view.ExpiresAt, view.IsExpired, view.IsClosed, view.AlreadyAnswered);

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

    // D11: cada faceta de lista chega como o parâmetro repetido
    // (?dc=DC1&dc=DC2). Lista vazia é ausência de filtro, não filtro que não
    // casa com nada.
    public static IReadOnlyList<T>? ParseFacet<T>(string[]? values, Func<string, T> parse)
        => values is null or { Length: 0 }
            ? null
            : values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(parse).Distinct().ToList() is { Count: > 0 } parsed
                ? parsed
                : null;

    // Estrito de propósito: o parse antigo caía em Dc1/Squad no valor
    // desconhecido, então ?dc=DC99 filtrava por DC1 sem avisar ninguém.
    public static DeliveryCenter ParseDc(string value) => value.Trim().ToUpperInvariant() switch
    {
        "DC1" => DeliveryCenter.Dc1,
        "DC2" => DeliveryCenter.Dc2,
        "DC3" => DeliveryCenter.Dc3,
        "DC4" => DeliveryCenter.Dc4,
        "DC5" => DeliveryCenter.Dc5,
        "DC6" => DeliveryCenter.Dc6,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid delivery center.")
    };

    public static ProjectType ParseProjectType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "squad" => ProjectType.Squad,
        "escopo fechado" or "fixedscope" or "fixed scope" => ProjectType.FixedScope,
        "alocação" or "alocacao" or "staffing" => ProjectType.Staffing,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid project type.")
    };

    public static NpsCollectionStatus ParseCollectionStatus(string value) => value.Trim().ToLowerInvariant() switch
    {
        "respondido" or "answered" => NpsCollectionStatus.Answered,
        "link gerado" or "linksent" or "link sent" => NpsCollectionStatus.LinkSent,
        "pendente" or "pending" => NpsCollectionStatus.Pending,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid NPS collection status.")
    };

    public static NpsClassification ParseClassification(string value)
        => ParseClassificationOrNull(value) ?? throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid NPS classification.");

    public static NpsFilterOptionsResponse ToResponse(NpsFilterOptionsView view)
        => new(view.Companies, view.DeliveryManagers);
}
