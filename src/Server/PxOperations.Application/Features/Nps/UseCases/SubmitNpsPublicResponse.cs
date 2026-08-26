using PxOperations.Application.Abstractions;
using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed record SubmitNpsPublicResponseCommand(
    int Score,
    int? BusinessValue,
    int? Schedule,
    int? Quality,
    int? Communication,
    string? Tags,
    string? Comment,
    string? RespondentName,
    string? RespondentEmail);

public sealed class SubmitNpsPublicResponseUseCase(INpsRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<NpsResponseView> ExecuteAsync(Guid token, SubmitNpsPublicResponseCommand command, CancellationToken ct)
    {
        var target = await repository.GetTargetByTokenAsync(token, ct);
        if (target is null)
        {
            throw new KeyNotFoundException("NPS token not found.");
        }

        if (target.Dispatch.Status == NpsDispatchStatus.Closed)
        {
            throw new InvalidOperationException("Dispatch is closed.");
        }

        // B12/D7: passado o prazo o link não coleta mais. O formulário já avisa
        // antes de perguntar, mas a regra tem de valer no servidor também.
        if (target.Dispatch.IsExpired(DateTimeOffset.UtcNow))
        {
            throw new InvalidOperationException("This NPS link has expired.");
        }

        // B2/D1: o link compartilhado existe para receber N respostas — uma por
        // pessoa do grupo do cliente. Só o link nominal segue de uso único.
        if (!target.IsGeneric && await repository.TargetHasResponseAsync(target.Id, ct))
        {
            throw new InvalidOperationException("This NPS link has already been answered.");
        }

        var isComplete = target.Dispatch.Format == NpsFormFormat.Complete;
        var response = SurveyResponse.Submit(
            target.ProjectId,
            target.DispatchId,
            target.Id,
            target.ContactId,
            command.Score,
            isComplete ? command.BusinessValue : null,
            isComplete ? command.Schedule : null,
            isComplete ? command.Quality : null,
            isComplete ? command.Communication : null,
            isComplete ? command.Tags : null,
            command.Comment,
            command.RespondentName,
            command.RespondentEmail,
            DateTimeOffset.UtcNow);

        repository.AddResponse(response);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (repository.IsDuplicateResponseException(ex))
        {
            throw new InvalidOperationException("This NPS link has already been answered.", ex);
        }

        return (await repository.ListResponsesAsync(target.DispatchId, new NpsFilter(null, null, null, null, null, null, null, null), ct))
            .Single(r => r.Id == response.Id);
    }
}
