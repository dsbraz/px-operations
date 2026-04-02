using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.ProjectHealth;

public partial class ProjectHealthFormPage : ComponentBase
{
    [Inject] private ProjectHealthClient ProjectHealthClient { get; set; } = default!;
    [Inject] private ProjectsClient ProjectsClient { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public int? Id { get; set; }

    private string formEmail = "";
    private string formDc = "";
    private int formProjectId;
    private string formSubProject = "";
    private bool[] practiceChecks = new bool[5];
    private string? formScope;
    private string? formSchedule;
    private string? formQuality;
    private string? formSatisfaction;
    private bool? formExpansion;
    private string formExpansionComment = "";
    private bool? formActionPlan;
    private string formHighlights = "";

    private List<ProjectResponse> allProjects = [];
    private List<ProjectResponse> filteredProjects = [];
    private string? error;
    private bool isSaving;
    private bool submitted;

    protected override async Task OnInitializedAsync()
    {
        allProjects = (await ProjectsClient.ListAsync(null, null, "InProgress", null, null, default)).ToList();

        if (Id.HasValue)
        {
            var entry = await ProjectHealthClient.GetById2Async(Id.Value, default);
            formEmail = entry.ReporterEmail;
            formDc = entry.ProjectDc;
            formProjectId = entry.ProjectId;
            formSubProject = entry.SubProject ?? "";
            practiceChecks = Enumerable.Range(0, 5).Select(i => i < entry.PracticesCount).ToArray();
            formScope = MapRagToValue(entry.Scope);
            formSchedule = MapRagToValue(entry.Schedule);
            formQuality = MapRagToValue(entry.Quality);
            formSatisfaction = MapRagToValue(entry.Satisfaction);
            formExpansion = entry.ExpansionOpportunity;
            formExpansionComment = entry.ExpansionComment ?? "";
            formActionPlan = entry.ActionPlanNeeded;
            formHighlights = entry.Highlights;
            OnDcChanged();
        }
    }

    private void OnDcChanged()
    {
        filteredProjects = string.IsNullOrEmpty(formDc)
            ? []
            : allProjects.Where(p => string.Equals(p.Dc, formDc, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!filteredProjects.Any(p => p.Id == formProjectId))
            formProjectId = 0;
    }

    private void TogglePractice(int index) => practiceChecks[index] = !practiceChecks[index];

    private string? GetRagValue(string key) => key switch
    {
        "scope" => formScope,
        "schedule" => formSchedule,
        "quality" => formQuality,
        "satisfaction" => formSatisfaction,
        _ => null
    };

    private void SetRagValue(string key, string value)
    {
        switch (key)
        {
            case "scope": formScope = value; break;
            case "schedule": formSchedule = value; break;
            case "quality": formQuality = value; break;
            case "satisfaction": formSatisfaction = value; break;
        }
    }

    private int ProgressPercent
    {
        get
        {
            var checks = new[]
            {
                !string.IsNullOrWhiteSpace(formEmail),
                !string.IsNullOrEmpty(formDc),
                formProjectId > 0,
                practiceChecks.Any(c => c),
                formScope is not null,
                formSchedule is not null,
                formQuality is not null,
                formSatisfaction is not null,
                formExpansion.HasValue,
                formActionPlan.HasValue,
                !string.IsNullOrWhiteSpace(formHighlights)
            };
            return (int)Math.Round(100.0 * checks.Count(c => c) / checks.Length);
        }
    }

    private async Task HandleSubmit()
    {
        error = null;

        if (string.IsNullOrWhiteSpace(formEmail)) { error = "Informe seu e-mail."; return; }
        if (formProjectId <= 0) { error = "Selecione um projeto."; return; }
        if (formScope is null || formSchedule is null || formQuality is null || formSatisfaction is null)
        { error = "Avalie todas as 4 dimensões."; return; }
        if (!formExpansion.HasValue) { error = "Informe se há oportunidade de expansão."; return; }
        if (!formActionPlan.HasValue) { error = "Informe se será preciso um plano de ação."; return; }
        if (string.IsNullOrWhiteSpace(formHighlights)) { error = "Informe os destaques da semana."; return; }

        var week = GetCurrentMonday().ToString("yyyy-MM-dd");
        var practicesCount = practiceChecks.Count(c => c);

        isSaving = true;
        try
        {
            if (Id.HasValue)
            {
                await ProjectHealthClient.Update2Async(Id.Value, new UpdateProjectHealthRequest
                {
                    ProjectId = formProjectId,
                    SubProject = string.IsNullOrWhiteSpace(formSubProject) ? null : formSubProject,
                    Week = week,
                    ReporterEmail = formEmail,
                    PracticesCount = practicesCount,
                    Scope = formScope,
                    Schedule = formSchedule,
                    Quality = formQuality,
                    Satisfaction = formSatisfaction,
                    ExpansionOpportunity = formExpansion.Value,
                    ExpansionComment = formExpansion.Value ? formExpansionComment : null,
                    ActionPlanNeeded = formActionPlan.Value,
                    Highlights = formHighlights
                }, default);
            }
            else
            {
                await ProjectHealthClient.Create2Async(new CreateProjectHealthRequest
                {
                    ProjectId = formProjectId,
                    SubProject = string.IsNullOrWhiteSpace(formSubProject) ? null : formSubProject,
                    Week = week,
                    ReporterEmail = formEmail,
                    PracticesCount = practicesCount,
                    Scope = formScope,
                    Schedule = formSchedule,
                    Quality = formQuality,
                    Satisfaction = formSatisfaction,
                    ExpansionOpportunity = formExpansion.Value,
                    ExpansionComment = formExpansion.Value ? formExpansionComment : null,
                    ActionPlanNeeded = formActionPlan.Value,
                    Highlights = formHighlights
                }, default);
            }

            submitted = true;
        }
        catch (ApiException ex)
        {
            error = ApiErrorFormatter.Format(ex, "Erro ao enviar resposta.");
        }
        finally
        {
            isSaving = false;
        }
    }

    private void ResetForm()
    {
        formEmail = "";
        formDc = "";
        formProjectId = 0;
        formSubProject = "";
        practiceChecks = new bool[5];
        formScope = null;
        formSchedule = null;
        formQuality = null;
        formSatisfaction = null;
        formExpansion = null;
        formExpansionComment = "";
        formActionPlan = null;
        formHighlights = "";
        filteredProjects = [];
        error = null;
        submitted = false;
    }

    private static DateOnly GetCurrentMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-daysFromMonday);
    }

    private static string MapRagToValue(string rag) => rag.ToLowerInvariant() switch
    {
        "verde" or "green" => "verde",
        "amarelo" or "yellow" => "amarelo",
        "vermelho" or "red" => "vermelho",
        _ => "verde"
    };

    private static readonly string[] PracticeLabels =
    [
        "Ata, status de projeto e atualizações de cronograma realizados e compartilhados com o cliente.",
        "Documentação trabalhada na semana atualizada e organizada (Drive, Figma, Mural, etc.)",
        "Jira com as principais entregas do projeto atualizado com suas respectivas datas de início e fim.",
        "Rituais foram executados na semana (dailys, weeklies, reviews).",
        "Apresentações com o cliente (de marco ou entrega) agendadas."
    ];

    private record RagDimension(string Number, string Key, string Label, string Description,
        Func<string, string> GetDescription);

    private static readonly RagDimension[] RagDimensions =
    [
        new("05", "scope", "Escopo",
            "Controle e aderência ao escopo definido — mudanças formais vs. improvisações.",
            v => v switch { "verde" => "Sem mudanças ou mudanças previstas e aceitas",
                "amarelo" => "Pequenas mudanças sem formalização",
                _ => "Mudanças relevantes fora de contrato" }),
        new("06", "schedule", "Cronograma",
            "Aderência ao planejamento — % de entregas concluídas no prazo.",
            v => v switch { "verde" => ">90% das entregas no prazo",
                "amarelo" => "70–90% no prazo",
                _ => "<70% no prazo ou atraso crítico" }),
        new("07", "quality", "Qualidade",
            "Clareza de objetivos, qualidade da narrativa e embasamento técnico.",
            v => v switch { "verde" => "Qualidade satisfatória em todos os critérios",
                "amarelo" => "Pontos de melhoria em pelo menos 1 critério",
                _ => "Estado crítico, requer ação imediata" }),
        new("08", "satisfaction", "Satisfação e Relacionamento",
            "Percepção das interações com o cliente — comunicação, participação e NPS.",
            v => v switch { "verde" => "Relacionamento excelente ou NPS 9–10",
                "amarelo" => "Interações com pouca proximidade, NPS neutro",
                _ => "Reclamações, NPS < 6 ou risco de churn" })
    ];

    private record RagOption(string Value, string Label, string CssClass);

    private static readonly RagOption[] RagOptions =
    [
        new("verde", "Verde", "rag-green"),
        new("amarelo", "Amarelo", "rag-yellow"),
        new("vermelho", "Vermelho", "rag-red")
    ];
}
