using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Projects;

namespace PxOperations.Infrastructure.Features.Nps;

/// <summary>
/// Traduz agregados e snapshots nas views que a API publica, incluindo os
/// rótulos e tons que a tela exibe.
/// </summary>
internal static class NpsViewMappings
{
    internal static NpsProjectView ToProjectView(NpsProjectSnapshot snapshot, DateTimeOffset now)
    {
        var stage = snapshot.Stage(now);
        var openStates = snapshot.OpenStates;
        var domainAction = NpsCollectionPolicy.DeterminePrimaryAction(
            stage,
            openStates,
            snapshot.MostRecentFormat,
            now);
        var links = snapshot.OpenDispatches
            .Select(dispatch => ToLinkView(dispatch, now))
            .Where(link => link is not null)
            .Cast<NpsLinkView>()
            .OrderBy(link => link.Format)
            .ToArray();

        return new NpsProjectView(
            snapshot.Project.Id,
            snapshot.Project.Name,
            snapshot.Project.Client,
            Dc(snapshot.Project.Dc),
            snapshot.Project.DeliveryManager,
            ProjectTypeLabel(snapshot.Project.Type),
            snapshot.Responses.Count,
            Stage(stage),
            Temporal(snapshot, stage, domainAction, now),
            snapshot.Collection?.IsWaived == true
                ? new NpsWaiverView(snapshot.Collection.WaiverReason!, snapshot.Collection.WaivedAt!.Value)
                : null,
            links,
            PrimaryAction(domainAction, stage, links),
            snapshot.IsOverdue(now),
            snapshot.LastDispatchClosedAt);
    }

    internal static NpsLinkView? ToLinkView(Dispatch dispatch, DateTimeOffset now)
    {
        var target = dispatch.Targets.FirstOrDefault(item => item.IsGeneric);
        if (target is null)
        {
            return null;
        }

        var expired = NpsCollectionPolicy.IsExpired(dispatch.ExpiresAt, now);
        var warning = NpsCollectionPolicy.IsExpiringSoon(dispatch.ExpiresAt, now);
        return new NpsLinkView(
            dispatch.Id,
            target.Token,
            NpsCodes.Format(dispatch.Format),
            NpsCodes.FormatLabel(dispatch.Format),
            dispatch.ExpiresAt,
            expired ? "expired" : "open",
            expired ? "Expirado" : "Aberto",
            expired ? "critical" : warning ? "warning" : "neutral");
    }

    internal static NpsBadgeView Stage(NpsCollectionStage stage) => stage switch
    {
        NpsCollectionStage.NoLink => new("no_link", "Sem link", "neutral"),
        NpsCollectionStage.AwaitingResponse => new("awaiting_response", "Aguardando resposta", "info"),
        NpsCollectionStage.Recollection => new("recollection", "Recoleta", "warning"),
        NpsCollectionStage.Current => new("current", "Em dia", "positive"),
        _ => new("waived", "Dispensado", "neutral")
    };

    internal static NpsBadgeView ProjectResultStatus(NpsProjectResultStatus status) => status switch
    {
        NpsProjectResultStatus.Responded => new("responded", "Respondido", "positive"),
        NpsProjectResultStatus.LinkGenerated => new("link_generated", "Link gerado", "info"),
        _ => new("pending", "Pendente", "neutral")
    };

    internal static NpsTemporalView Temporal(
        NpsProjectSnapshot snapshot,
        NpsCollectionStage stage,
        NpsPrimaryAction? action,
        DateTimeOffset now)
    {
        if (stage == NpsCollectionStage.Waived)
        {
            var at = snapshot.Collection!.WaivedAt!.Value;
            return new NpsTemporalView($"Dispensado em {at:dd/MM/yyyy}", "neutral", at);
        }

        if (stage == NpsCollectionStage.AwaitingResponse && action?.DispatchId is int dispatchId)
        {
            var dispatch = snapshot.OpenDispatches.Single(item => item.Id == dispatchId);
            if (NpsCollectionPolicy.IsExpired(dispatch.ExpiresAt, now))
            {
                return new NpsTemporalView($"Link expirado há {Days(now - dispatch.ExpiresAt)}d", "critical", dispatch.ExpiresAt);
            }

            var tone = NpsCollectionPolicy.IsExpiringSoon(dispatch.ExpiresAt, now) ? "warning" : "neutral";
            return new NpsTemporalView($"Expira em {Math.Max(1, (int)Math.Ceiling((dispatch.ExpiresAt - now).TotalDays))}d", tone, dispatch.ExpiresAt);
        }

        if (stage == NpsCollectionStage.NoLink)
        {
            return snapshot.LastDispatchClosedAt is { } closedAt
                ? new NpsTemporalView($"Sem link há {Days(now - closedAt)}d", "neutral", closedAt)
                : new NpsTemporalView("Nunca coletado", "neutral", null);
        }

        var lastResponseAt = snapshot.LastResponseAt;
        return new NpsTemporalView($"Última resposta há {Days(now - lastResponseAt!.Value)}d", "neutral", lastResponseAt);
    }

    internal static NpsPrimaryActionView? PrimaryAction(
        NpsPrimaryAction? action,
        NpsCollectionStage stage,
        IReadOnlyList<NpsLinkView> links)
    {
        if (action is null)
        {
            return null;
        }

        var code = action.Kind switch
        {
            NpsPrimaryActionKind.Reactivate => "reactivate",
            NpsPrimaryActionKind.CopyLink => "copy_link",
            _ => "generate_link"
        };
        var label = action.Kind switch
        {
            NpsPrimaryActionKind.Reactivate => "Reativar",
            NpsPrimaryActionKind.CopyLink => "Copiar link",
            _ when stage == NpsCollectionStage.AwaitingResponse => "Gerar novo link",
            _ => "Gerar link"
        };
        var link = action.DispatchId.HasValue ? links.FirstOrDefault(item => item.DispatchId == action.DispatchId.Value) : null;
        return new NpsPrimaryActionView(
            code,
            label,
            action.Format.HasValue ? NpsCodes.Format(action.Format.Value) : null,
            action.DispatchId,
            link?.Token);
    }

    internal static (string Code, string Label, string Tone) DispatchAvailability(Dispatch dispatch, DateTimeOffset now)
    {
        if (!dispatch.IsOpen)
        {
            return ("closed", "Encerrado", "neutral");
        }

        if (NpsCollectionPolicy.IsExpired(dispatch.ExpiresAt, now))
        {
            return ("expired", "Expirado", "critical");
        }

        return NpsCollectionPolicy.IsExpiringSoon(dispatch.ExpiresAt, now)
            ? ("open", "Aberto", "warning")
            : ("open", "Aberto", "positive");
    }

    internal static string PublicAvailability(NpsCollection collection, Dispatch dispatch, bool answered, DateTimeOffset now)
    {
        if (collection.IsWaived)
        {
            return "waived";
        }

        if (!dispatch.IsOpen)
        {
            return "closed";
        }

        if (NpsCollectionPolicy.IsExpired(dispatch.ExpiresAt, now))
        {
            return "expired";
        }

        return answered ? "already_answered" : "open";
    }

    internal static IReadOnlyList<NpsAspectView> Aspects(NpsLanguage language)
    {
        var labels = language switch
        {
            NpsLanguage.English => new[] { "Quality", "Schedule", "Communication", "Business value" },
            NpsLanguage.Spanish => new[] { "Calidad", "Plazo", "Comunicación", "Valor para el negocio" },
            _ => new[] { "Qualidade", "Prazo", "Comunicação", "Valor para o negócio" }
        };
        var codes = new[] { "quality", "schedule", "communication", "businessValue" };
        return codes.Select((code, index) => new NpsAspectView(
            code,
            labels[index],
            new NpsScaleView(NpsScale.MinimumAspect, NpsScale.MaximumAspect))).ToArray();
    }

    internal static NpsDistributionView Distribution(
        NpsClassification classification,
        IReadOnlyDictionary<NpsClassification, int> counts,
        decimal percentage)
        => new(
            NpsCodes.Classification(classification),
            NpsCodes.ClassificationLabel(classification),
            classification switch
            {
                NpsClassification.Detractor => "critical",
                NpsClassification.Passive => "warning",
                _ => "positive"
            },
            counts.GetValueOrDefault(classification),
            percentage);

    internal static NpsAspectAverageView Aspect(
        string code,
        string label,
        IReadOnlyList<SurveyResponse> completeResponses,
        Func<SurveyResponse, int?> aspect)
    {
        var values = completeResponses
            .Select(aspect)
            .Where(value => value.HasValue)
            .Select(value => (decimal)value!.Value)
            .ToArray();

        return new NpsAspectAverageView(
            code,
            label,
            values.Length == 0 ? null : decimal.Round(values.Average(), 1, MidpointRounding.AwayFromZero),
            values.Length);
    }

    internal static IReadOnlyList<NpsOptionView> Options(IEnumerable<string?> values)
        => values.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
            .Select(value => new NpsOptionView(value, value))
            .ToArray();

    internal static IReadOnlyList<NpsOptionView> Options(IEnumerable<string> codes, IEnumerable<string> labels)
        => codes.Zip(labels)
            .Distinct()
            .Select(pair => new NpsOptionView(pair.First, pair.Second))
            .OrderBy(option => option.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    internal static int Days(TimeSpan span) => Math.Max(0, (int)Math.Floor(span.TotalDays));

    internal static string Dc(DeliveryCenter value) => value.ToString().ToUpperInvariant();

    internal static string ProjectTypeCode(ProjectType value) => value switch
    {
        ProjectType.Squad => "squad",
        ProjectType.FixedScope => "fixed_scope",
        _ => "staffing"
    };

    internal static string ProjectTypeLabel(ProjectType value) => value switch
    {
        ProjectType.Squad => "Squad",
        ProjectType.FixedScope => "Escopo fechado",
        _ => "Staffing"
    };
}
