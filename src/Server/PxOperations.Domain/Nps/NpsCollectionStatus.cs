namespace PxOperations.Domain.Nps;

/// <summary>
/// Estado da COLETA de um projeto, não do projeto. D9 é explícito: o NPS não
/// tem "projeto encerrado"; o único estado que tira um card da vista é a
/// coleta dispensada, que é reversível e vive à parte, no toggle de F1.
///
/// São três, como o PRD descreve o CSV de F11. A tela derivava um quarto,
/// "Sem link", a partir de !IsOverdue — inalcançável, porque projeto sem
/// disparo vivo e sem resposta recente é sempre vencido, por definição da
/// própria regra de vencimento.
/// </summary>
public enum NpsCollectionStatus
{
    Pending = 0,
    LinkSent = 1,
    Answered = 2
}
