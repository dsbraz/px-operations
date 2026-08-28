using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsPublicPage : ComponentBase
{
    [Parameter] public Guid Token { get; set; }
    [Inject] private NpsClient NpsClient { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private readonly Dictionary<string, int> aspects = new(StringComparer.Ordinal);
    private NpsPublicSurveyView? survey;
    private bool isLoading = true;
    private bool invalidToken;
    private bool submitted;
    private bool isSubmitting;
    private int? score;
    private string? comment;
    private string? respondentName;
    private string? respondentEmail;
    private string? submitError;

    private PublicTexts Texts => PublicTexts.For(survey?.Language);
    private IEnumerable<int> ScoreValues => survey is null
        ? []
        : Enumerable.Range(survey.ScoreScale.Minimum, survey.ScoreScale.Maximum - survey.ScoreScale.Minimum + 1);
    private string StorageKey => $"px-operations:nps:{Token}:submitted";
    private string FinalTitle => survey?.Availability switch
    {
        "expired" => Texts.ExpiredTitle,
        "already_answered" => Texts.AnsweredTitle,
        _ => Texts.UnavailableTitle
    };
    private string FinalMessage => survey?.Availability switch
    {
        "expired" => Texts.ExpiredMessage,
        "already_answered" => Texts.AnsweredMessage,
        _ => Texts.UnavailableMessage
    };

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        invalidToken = false;
        try
        {
            survey = await NpsClient.GetPublicAsync(Token);
            if (survey.IsGeneric && survey.Availability == "open")
            {
                submitted = await JsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey) is not null;
            }
        }
        catch (ApiException exception) when (exception.StatusCode == 404)
        {
            invalidToken = true;
        }
        finally
        {
            isLoading = false;
        }
    }

    private int? AspectValue(string code) => aspects.GetValueOrDefault(code);
    private void SetAspect(string code, int value) => aspects[code] = value;

    private async Task SubmitAsync()
    {
        if (!score.HasValue || survey is null)
        {
            return;
        }

        isSubmitting = true;
        submitError = null;
        try
        {
            await NpsClient.SubmitPublicAsync(Token, new SubmitNpsSurveyResponseRequest
            {
                Score = score.Value,
                Quality = AspectValue("quality"),
                Schedule = AspectValue("schedule"),
                Communication = AspectValue("communication"),
                BusinessValue = AspectValue("businessValue"),
                Comment = comment,
                RespondentName = respondentName,
                RespondentEmail = respondentEmail
            });
            submitted = true;
            if (survey.IsGeneric)
            {
                await JsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, "true");
            }
        }
        catch (ApiException exception) when (exception.StatusCode == 409)
        {
            submitError = Texts.DuplicateEmail;
        }
        catch (ApiException exception) when (exception.StatusCode == 429)
        {
            submitError = Texts.TooManyAttempts;
        }
        catch (Exception)
        {
            submitError = Texts.SubmitError;
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private sealed record PublicTexts(
        string Loading,
        string Introduction,
        string ValidUntil,
        string Question,
        string Unlikely,
        string Likely,
        string Comment,
        string Name,
        string Email,
        string PrivacyTitle,
        string PrivacyMessage,
        string Submit,
        string SuccessTitle,
        string SuccessMessage,
        string ExpiredTitle,
        string ExpiredMessage,
        string UnavailableTitle,
        string UnavailableMessage,
        string AnsweredTitle,
        string AnsweredMessage,
        string InvalidTitle,
        string InvalidMessage,
        string DuplicateEmail,
        string TooManyAttempts,
        string SubmitError)
    {
        public static PublicTexts For(string? language) => language switch
        {
            "en" => English,
            "es" => Spanish,
            _ => Portuguese
        };

        private static readonly PublicTexts Portuguese = new(
            "Carregando...", "Pesquisa de relacionamento", "Válido até", "Qual a probabilidade de você recomendar a BRQ?",
            "Pouco provável", "Muito provável", "Comentário", "Nome (opcional)", "E-mail (opcional)", "Privacidade",
            "Nome e e-mail são opcionais e usados apenas para contextualizar sua resposta.", "Enviar resposta", "Obrigado!",
            "Sua resposta foi registrada.", "Link expirado", "Este link expirou.", "Link indisponível",
            "Esta pesquisa não está disponível.", "Resposta já registrada", "Este link de contato já foi respondido.",
            "Link inválido", "Não encontramos esta pesquisa.", "Este e-mail já respondeu a este link.",
            "Muitas tentativas em pouco tempo. Tente novamente mais tarde.", "Não foi possível enviar sua resposta.");

        private static readonly PublicTexts English = new(
            "Loading...", "Relationship survey", "Valid until", "How likely are you to recommend BRQ?",
            "Not likely", "Very likely", "Comment", "Name (optional)", "Email (optional)", "Privacy",
            "Name and email are optional and only used to add context to your response.", "Submit response", "Thank you!",
            "Your response has been recorded.", "Expired link", "This link has expired.", "Unavailable link",
            "This survey is unavailable.", "Response already recorded", "This contact link has already been answered.",
            "Invalid link", "We could not find this survey.", "This email has already answered this link.",
            "Too many attempts. Please try again later.", "We could not submit your response.");

        private static readonly PublicTexts Spanish = new(
            "Cargando...", "Encuesta de relación", "Válido hasta", "¿Qué probabilidad hay de que recomiendes BRQ?",
            "Poco probable", "Muy probable", "Comentario", "Nombre (opcional)", "Correo (opcional)", "Privacidad",
            "El nombre y el correo son opcionales y solo se usan para contextualizar tu respuesta.", "Enviar respuesta", "¡Gracias!",
            "Tu respuesta fue registrada.", "Enlace expirado", "Este enlace expiró.", "Enlace no disponible",
            "Esta encuesta no está disponible.", "Respuesta ya registrada", "Este enlace de contacto ya fue respondido.",
            "Enlace inválido", "No encontramos esta encuesta.", "Este correo ya respondió a este enlace.",
            "Demasiados intentos. Inténtalo de nuevo más tarde.", "No fue posible enviar tu respuesta.");
    }
}
