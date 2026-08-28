using PxOperations.Application.Abstractions;
using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed record SubmitNpsPublicResponseCommand(
    Guid Token,
    int Score,
    int? Quality,
    int? Schedule,
    int? Communication,
    int? BusinessValue,
    string? Comment,
    string? RespondentName,
    string? RespondentEmail);

public sealed class SubmitNpsPublicResponseUseCase(
    INpsRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<int> ExecuteAsync(SubmitNpsPublicResponseCommand command, CancellationToken ct)
    {
        var normalizedEmail = string.IsNullOrWhiteSpace(command.RespondentEmail)
            ? null
            : command.RespondentEmail.Trim().ToLowerInvariant();
        var context = await repository.GetResponseContextAsync(command.Token, normalizedEmail, ct)
            ?? throw new ResourceNotFoundException("NPS link was not found.");

        var response = SurveyResponse.Submit(
            context,
            command.Score,
            command.Quality,
            command.Schedule,
            command.Communication,
            command.BusinessValue,
            command.Comment,
            command.RespondentName,
            command.RespondentEmail,
            timeProvider.GetUtcNow());

        repository.AddResponse(response);
        await unitOfWork.SaveChangesAsync(ct);
        return response.Id;
    }
}
