using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsPublicPage : ComponentBase
{
    [Parameter] public Guid Token { get; set; }
    [Inject] private NpsClient NpsClient { get; set; } = default!;

    private NpsPublicSurveyResponse? survey;
    private bool isLoading = true;
    private bool submitted;
    private string? loadError;

    private int score = 10;
    private int? scope;
    private int? schedule;
    private int? quality;
    private int? communication;
    private string? comment;
    private string? respondentName;
    private string? respondentEmail;
    private NpsPublicFormTexts Texts => NpsPublicFormTexts.For(survey?.Language);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            survey = await NpsClient.GetPublicAsync(Token);
            loadError = survey is null ? "Link de NPS não encontrado." : null;
        }
        catch (Exception)
        {
            loadError = "Não foi possível carregar o formulário NPS.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SubmitAsync()
    {
        try
        {
            await NpsClient.SubmitPublicAsync(Token, new SubmitNpsSurveyResponseRequest
            {
                Score = score,
                Scope = scope,
                Schedule = schedule,
                Quality = quality,
                Communication = communication,
                Tags = null,
                Comment = comment,
                RespondentName = respondentName,
                RespondentEmail = respondentEmail
            });
            submitted = true;
        }
        catch (Exception)
        {
            loadError = Texts.SubmitError;
        }
    }

    private sealed record NpsPublicFormTexts(
        string Loading,
        string ThankYouTitle,
        string ThankYouMessage,
        string ProjectContext,
        string AlreadyAnswered,
        string NpsQuestion,
        string ScaleLow,
        string ScaleHigh,
        string Scope,
        string Schedule,
        string Quality,
        string Communication,
        string CommentPlaceholder,
        string OptionalIdentity,
        string NamePlaceholder,
        string EmailPlaceholder,
        string Submit,
        string SubmitError)
    {
        public static NpsPublicFormTexts For(string? language)
            => (language ?? "").Trim().ToLowerInvariant() switch
            {
                "inglês" or "ingles" or "english" or "en" => English,
                "espanhol" or "spanish" or "es" => Spanish,
                _ => Portuguese
            };

        private static readonly NpsPublicFormTexts Portuguese = new(
            "Carregando...",
            "Obrigado",
            "Sua resposta foi registrada.",
            "Projeto avaliado",
            "Este link já recebeu uma resposta.",
            "Qual a probabilidade de você recomendar a BRQ?",
            "Pouco provável",
            "Muito provável",
            "Escopo",
            "Prazo",
            "Qualidade",
            "Comunicação",
            "Comentário",
            "Identificação opcional",
            "Seu nome",
            "Seu email",
            "Enviar resposta",
            "Não foi possível enviar sua resposta.");

        private static readonly NpsPublicFormTexts English = new(
            "Loading...",
            "Thank you",
            "Your response has been recorded.",
            "Project being evaluated",
            "This link has already received a response.",
            "How likely are you to recommend BRQ?",
            "Not likely",
            "Very likely",
            "Scope",
            "Schedule",
            "Quality",
            "Communication",
            "Comment",
            "Optional identification",
            "Your name",
            "Your email",
            "Submit response",
            "We could not submit your response.");

        private static readonly NpsPublicFormTexts Spanish = new(
            "Cargando...",
            "Gracias",
            "Tu respuesta fue registrada.",
            "Proyecto evaluado",
            "Este enlace ya recibió una respuesta.",
            "¿Qué probabilidad hay de que recomiendes BRQ?",
            "Poco probable",
            "Muy probable",
            "Alcance",
            "Plazo",
            "Calidad",
            "Comunicación",
            "Comentario",
            "Identificación opcional",
            "Tu nombre",
            "Tu email",
            "Enviar respuesta",
            "No fue posible enviar tu respuesta.");
    }
}
