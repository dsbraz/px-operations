namespace PxOperations.Application.Features.Nps;

public sealed record NpsDashboardView(
    int TotalProjects,
    int OverdueProjects,
    int ActiveDispatches,
    int TotalResponses,
    decimal OfficialNps,
    decimal AverageScore,
    int Detractors,
    int Passives,
    int Promoters,
    // B11/F9: médias por aspecto, escala de 1 a 5 (D10). Cada aspecto é
    // OPCIONAL mesmo no formato Completo, então cada um leva o seu próprio
    // denominador: dividir todos pelo total de respostas puxaria para baixo
    // justamente o aspecto que as pessoas pularam. Média nula quando ninguém
    // respondeu — zero seria uma nota, e a escala começa em 1.
    int CompleteResponses,
    decimal? QualityAverage, int QualityCount,
    decimal? ScheduleAverage, int ScheduleCount,
    decimal? CommunicationAverage, int CommunicationCount,
    decimal? BusinessValueAverage, int BusinessValueCount);
