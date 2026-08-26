namespace PxOperations.Api.Features.Nps;

/// <summary>
/// B4: com o link compartilhado (D1) o uso único deixou de ser o freio. F4 pede
/// três proteções — navegador, e-mail e IP —, e o PRD as chama de
/// "proporcionais" de propósito: quem quiser burlar troca de navegador ou de
/// rede. Elas barram reenvio acidental e envio em massa, não um adversário
/// determinado. É risco aceito, registrado aqui para não passar por descuido.
/// </summary>
public static class AntiAbuse
{
    public const string SubmitPolicy = "nps-public-submit";

    /// <summary>
    /// Respostas por IP na janela. Alto o bastante para um escritório inteiro
    /// atrás do mesmo NAT responder à mesma pesquisa sem esbarrar no limite.
    /// </summary>
    public const int SubmitPermitLimit = 10;

    public static readonly TimeSpan SubmitWindow = TimeSpan.FromMinutes(1);
}
