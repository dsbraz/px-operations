using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsPublicPage : ComponentBase
{
    [Parameter] public Guid Token { get; set; }
    [Inject] private NpsClient NpsClient { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private NpsPublicSurveyResponse? survey;
    private bool isLoading = true;
    private bool submitted;
    private bool answeredInThisBrowser;
    private string? loadError;
    private string? submitError;

    // D10: a nota não nasce escolhida. Um 10 pré-marcado transformaria "não
    // respondi" em "sou promotor" e inflaria o NPS a cada envio distraído.
    private int? score;
    private int? businessValue;
    private int? schedule;
    private int? quality;
    private int? communication;
    private string? comment;
    private string? respondentName;
    private string? respondentEmail;

    private Texts T => Texts.For(survey?.Language);

    private bool Identified
        => !string.IsNullOrWhiteSpace(respondentName) || !string.IsNullOrWhiteSpace(respondentEmail);

    /// <summary>D7: o respondente vê até quando a pesquisa fica aberta.</summary>
    private string? Deadline
        => survey is not null && DateTimeOffset.TryParse(survey.ExpiresAt, out var expires)
            ? T.DeadlineText(expires.ToLocalTime().ToString("dd/MM/yyyy"))
            : null;

    /// <summary>
    /// Os quatro aspectos do formato Completo, com rótulo, dica e acesso ao
    /// campo. Existe para o markup não repetir o mesmo bloco quatro vezes.
    /// </summary>
    private IReadOnlyList<Dimension> Dimensions =>
    [
        new(T.DimQuality, T.DimQualityHint, () => quality, v => quality = v),
        new(T.DimSchedule, T.DimScheduleHint, () => schedule, v => schedule = v),
        new(T.DimCommunication, T.DimCommunicationHint, () => communication, v => communication = v),
        new(T.DimValue, T.DimValueHint, () => businessValue, v => businessValue = v)
    ];

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        loadError = null;

        try
        {
            survey = await NpsClient.GetPublicAsync(Token);
            answeredInThisBrowser = await HasAnsweredInThisBrowserAsync();
        }
        catch (Exception)
        {
            loadError = T.ErrorText;
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// Escolher uma nota apaga o aviso de nota obrigatória na hora: deixar o
    /// erro na tela depois de corrigido confunde.
    /// </summary>
    private void ChooseScore(int value)
    {
        score = value;
        submitError = null;
    }

    private async Task SubmitAsync()
    {
        submitError = null;

        // O protótipo barra o envio sem escolha em vez de mandar um padrão.
        if (score is null)
        {
            submitError = T.ScoreRequired;
            return;
        }

        try
        {
            await NpsClient.SubmitPublicAsync(Token, new SubmitNpsSurveyResponseRequest
            {
                Score = score.Value,
                BusinessValue = businessValue,
                Schedule = schedule,
                Quality = quality,
                Communication = communication,
                Tags = null,
                Comment = comment,
                RespondentName = respondentName,
                RespondentEmail = respondentEmail
            });
            submitted = true;
            await MarkAnsweredInThisBrowserAsync();
        }
        catch (ApiException apiException)
        {
            // Erro de envio não pode apagar o formulário: quem preencheu
            // perderia tudo. loadError substitui a tela; submitError fica junto
            // do botão. B4: cada freio tem a sua mensagem — "já respondi com
            // este e-mail" e "muitas respostas deste ponto" pedem reações
            // diferentes de quem está do outro lado.
            submitError = apiException.StatusCode switch
            {
                409 => await ConflictMessageAsync(),
                429 => T.TooManyRequests,
                _ => T.SubmitError
            };
        }
        catch (Exception)
        {
            submitError = T.SubmitError;
        }
    }

    /// <summary>
    /// O servidor devolve 409 para tudo que impede o envio: prazo vencido,
    /// disparo fechado e e-mail repetido. Adivinhar pelo código dizia "este
    /// e-mail já respondeu" a quem só pegou o link tarde demais — e apagar o
    /// e-mail não resolvia, deixando a pessoa sem saída.
    ///
    /// Em vez de inventar contrato, relê o estado do link: se ele fechou
    /// enquanto o formulário estava aberto, a tela troca para o aviso de prazo,
    /// que já existe. Sobrando, é o dedupe de e-mail.
    /// </summary>
    private async Task<string> ConflictMessageAsync()
    {
        try
        {
            var current = await NpsClient.GetPublicAsync(Token);
            if (current.IsExpired || current.IsClosed)
            {
                survey = current;
                return T.ClosedText;
            }
        }
        catch (Exception)
        {
            // Sem conseguir reler, a mensagem mais provável é a do dedupe: as
            // outras duas o formulário já teria barrado antes de perguntar.
        }

        return T.DuplicateEmail;
    }

    /// <summary>
    /// B4: o freio de navegador. É o mais fraco dos três de propósito — quem
    /// quiser burlar abre uma janela anônima —, e existe para barrar o reenvio
    /// distraído, não fraude. Vive no cliente porque o cookie teria de cruzar
    /// origem, e a API não usa credenciais.
    /// </summary>
    private string BrowserFlagKey => $"nps-answered-{Token}";

    private async Task<bool> HasAnsweredInThisBrowserAsync()
    {
        try
        {
            return await JsRuntime.InvokeAsync<string?>("localStorage.getItem", BrowserFlagKey) is not null;
        }
        catch (Exception)
        {
            // Janela anônima ou armazenamento bloqueado: sem a marca, mostrar o
            // formulário. Barrar quem talvez nem tenha respondido é pior.
            return false;
        }
    }

    private async Task MarkAnsweredInThisBrowserAsync()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("localStorage.setItem", BrowserFlagKey, "1");
        }
        catch (Exception)
        {
            // A resposta já foi gravada no servidor. Não conseguir marcar o
            // navegador não pode transformar um envio bem-sucedido em erro.
        }
    }

    private sealed record Dimension(string Label, string Hint, Func<int?> Value, Action<int> Set);

    /// <summary>
    /// Textos do formulário público, nos três idiomas que o backend aceita. Os
    /// de português e inglês são os do protótipo, palavra por palavra.
    /// </summary>
    private sealed record Texts(
        string Loading, string Eyebrow, string Title, string MetaTime, string MetaAnon,
        string Deadline, string Q1, string Q1Hint, string ScaleLow, string ScaleHigh, string ScoreOptionFormat,
        string Q2, string Q2Hint, string DimsLegend,
        string DimQuality, string DimQualityHint, string DimSchedule, string DimScheduleHint,
        string DimCommunication, string DimCommunicationHint, string DimValue, string DimValueHint,
        string Q3, string Q3Hint, string CommentLabel, string CommentPlaceholder,
        string OptionalEyebrow, string IdentTitle, string IdentHint,
        string NameLabel, string NamePlaceholder, string EmailLabel,
        string Submit, string ScoreRequired, string SubmitError,
        string SentTitle, string SentText, string SentPrivacyAnon, string SentPrivacyNamed,
        string SentShare, string SentClose,
        string ClosedTitle, string ClosedText, string ErrorTitle, string ErrorText,
        string AlreadyAnswered,
        // B4/F4: cada freio fala com o respondente de um jeito. Os três dizem
        // que o link SEGUE VALENDO para as outras pessoas — sem isso o aviso
        // parece encerrar a coleta, que é o oposto do link compartilhado (D1).
        string AnsweredInThisBrowser, string DuplicateEmail, string TooManyRequests,
        string FootPrivacy)
    {
        public string DeadlineText(string date) => Deadline.Replace("{date}", date);

        public string ScoreOption(int n, int max)
            => ScoreOptionFormat.Replace("{n}", n.ToString(CultureInfo.InvariantCulture))
                .Replace("{max}", max.ToString(CultureInfo.InvariantCulture));

        public static Texts For(string? language)
            => (language ?? "").Trim().ToLowerInvariant() switch
            {
                "inglês" or "ingles" or "english" or "en" => English,
                "espanhol" or "spanish" or "es" => Spanish,
                _ => Portuguese
            };

        private static readonly Texts Portuguese = new(
            "Carregando...", "Pesquisa de satisfação", "Sua opinião sobre o projeto",
            "Menos de 1 minuto", "Resposta anônima", "Aberta até {date}",
            "Qual a probabilidade de você recomendar a BRQ a um colega ou parceiro?",
            "Considere sua experiência geral com o time e com a entrega.",
            "Nada provável", "Extremamente provável", "Nota {n} de {max}",
            "Como você avalia estes aspectos da entrega?",
            "Opcional. Pule os aspectos que não se aplicam ao seu caso.",
            "Escala de 1 a 5: 1 é muito abaixo do esperado, 5 é excelente.",
            "Qualidade técnica da entrega", "Estabilidade, poucos defeitos, soluções bem construídas.",
            "Aderência aos prazos acordados", "Combinados cumpridos e desvios avisados a tempo.",
            "Comunicação, clareza e transparência", "Informação clara, na hora certa, sem surpresa.",
            "Valor gerado para o negócio", "O quanto a entrega ajudou no seu resultado.",
            "O que motivou a sua nota?",
            "Opcional. É o campo que mais ajuda o time a agir, pode escrever à vontade.",
            "Seu comentário", "O que funcionou bem e o que pode melhorar?",
            "Opcional", "Quer se identificar?",
            "Nenhum campo aqui é obrigatório. Sem preencher, sua resposta é enviada anônima; preencha só se quiser que o time possa voltar a falar com você.",
            "Seu nome (opcional)", "Como podemos te chamar", "Seu e-mail (opcional)",
            "Enviar avaliação", "Escolha uma nota de 1 a 10 para enviar.",
            "Não foi possível enviar sua resposta.",
            "Obrigado",
            "Obrigado pelo tempo. O que você escreveu chega ao time que toca o projeto. É assim que a gente descobre o que corrigir.",
            "Sua resposta foi registrada sem identificação: o time vê a nota e o comentário, não quem escreveu.",
            "Você se identificou: o time pode voltar a falar com você sobre esta avaliação.",
            "O mesmo link continua valendo: as outras pessoas do seu time ainda podem responder.",
            "Pode fechar esta página.",
            "O prazo desta pesquisa terminou",
            "O link de resposta vale 20 dias e esse prazo já passou. Se ainda quiser dar seu retorno, peça um link novo ao time da BRQ.",
            "Não encontramos esta avaliação",
            "O link pode ter expirado ou ter sido copiado pela metade. Confira o endereço ou peça um novo link ao time da BRQ.",
            "Este link já recebeu uma resposta.",
            "Você já respondeu por este navegador. O link continua valendo: as outras pessoas do seu time ainda podem responder.",
            "Este e-mail já respondeu esta pesquisa. Se quiser mandar outro retorno, deixe o campo de e-mail em branco.",
            "Recebemos muitas respostas deste ponto de acesso em pouco tempo. Aguarde um minuto e envie de novo.",
            "Nome e e-mail são opcionais e só são usados para dar retorno sobre esta avaliação.");

        private static readonly Texts English = Portuguese with
        {
            Loading = "Loading...", Eyebrow = "Satisfaction survey", Title = "Your feedback on the project",
            MetaTime = "Under 1 minute", MetaAnon = "Anonymous answer", Deadline = "Open until {date}",
            Q1 = "How likely are you to recommend BRQ to a colleague or partner?",
            Q1Hint = "Think about your overall experience with the team and the delivery.",
            ScaleLow = "Not likely", ScaleHigh = "Extremely likely", ScoreOptionFormat = "Score {n} of {max}",
            Q2 = "How do you rate these aspects of the delivery?",
            Q2Hint = "Optional. Skip the aspects that do not apply to you.",
            DimsLegend = "Scale from 1 to 5: 1 is well below expectations, 5 is excellent.",
            DimQuality = "Technical quality of the delivery",
            DimQualityHint = "Stability, few defects, well-built solutions.",
            DimSchedule = "Adherence to agreed deadlines",
            DimScheduleHint = "Commitments met and deviations flagged in time.",
            DimCommunication = "Communication, clarity and transparency",
            DimCommunicationHint = "Clear information, at the right time, no surprises.",
            DimValue = "Value generated for the business",
            DimValueHint = "How much the delivery helped your results.",
            Q3 = "What drove your score?",
            Q3Hint = "Optional. This is the field that helps the team most.",
            CommentLabel = "Your comment", CommentPlaceholder = "What worked well and what could improve?",
            OptionalEyebrow = "Optional", IdentTitle = "Would you like to identify yourself?",
            IdentHint = "Nothing here is required. Leave it blank and your answer stays anonymous.",
            NameLabel = "Your name (optional)", NamePlaceholder = "What should we call you",
            EmailLabel = "Your email (optional)",
            Submit = "Submit feedback", ScoreRequired = "Pick a score from 1 to 10 to submit.",
            SubmitError = "We could not submit your response.",
            SentTitle = "Thank you",
            SentText = "Thanks for your time. What you wrote reaches the team running the project.",
            SentPrivacyAnon = "Your answer was recorded without identification.",
            SentPrivacyNamed = "You identified yourself: the team can follow up with you.",
            SentShare = "The same link is still valid: others on your team can still answer.",
            SentClose = "You can close this page.",
            ClosedTitle = "This survey has closed",
            ClosedText = "The response link is valid for 20 days and that period has passed. Ask the BRQ team for a new link.",
            ErrorTitle = "We could not find this survey",
            ErrorText = "The link may have expired or been copied incompletely.",
            AlreadyAnswered = "This link has already received a response.",
            AnsweredInThisBrowser = "You have already answered from this browser. The link is still valid: others on your team can still answer.",
            DuplicateEmail = "This e-mail has already answered this survey. To send more feedback, leave the e-mail field blank.",
            TooManyRequests = "We received a lot of answers from this connection in a short time. Please wait a minute and send again.",
            FootPrivacy = "Name and email are optional."
        };

        private static readonly Texts Spanish = Portuguese with
        {
            Loading = "Cargando...", Eyebrow = "Encuesta de satisfacción", Title = "Tu opinión sobre el proyecto",
            MetaTime = "Menos de 1 minuto", MetaAnon = "Respuesta anónima", Deadline = "Abierta hasta {date}",
            Q1 = "¿Qué probabilidad hay de que recomiendes BRQ a un colega o socio?",
            ScaleLow = "Nada probable", ScaleHigh = "Extremadamente probable", ScoreOptionFormat = "Nota {n} de {max}",
            Q3 = "¿Qué motivó tu nota?", CommentLabel = "Tu comentario",
            IdentTitle = "¿Quieres identificarte?",
            NameLabel = "Tu nombre (opcional)", EmailLabel = "Tu correo (opcional)",
            Submit = "Enviar evaluación", ScoreRequired = "Elige una nota de 1 a 10 para enviar.",
            SubmitError = "No fue posible enviar tu respuesta.",
            SentTitle = "Gracias",
            ClosedTitle = "El plazo de esta encuesta terminó",
            ErrorTitle = "No encontramos esta evaluación",
            AlreadyAnswered = "Este enlace ya recibió una respuesta.",
            AnsweredInThisBrowser = "Ya respondiste desde este navegador. El enlace sigue válido: las demás personas de tu equipo aún pueden responder.",
            DuplicateEmail = "Este correo ya respondió esta encuesta. Si quieres enviar otro comentario, deja el campo de correo en blanco.",
            TooManyRequests = "Recibimos muchas respuestas desde esta conexión en poco tiempo. Espera un minuto y envía de nuevo."
        };
    }
}
