using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps;

public interface INpsRepository
{
    Task<bool> ProjectExistsAsync(int projectId, CancellationToken ct);
    Task<bool> ContactBelongsToProjectAsync(int projectId, int contactId, CancellationToken ct);
    Task<Contact?> GetContactAsync(int id, CancellationToken ct);
    void AddContact(Contact contact);
    Task<NpsCollection?> GetCollectionAsync(int projectId, CancellationToken ct);
    Task<NpsCollection> GetOrCreateCollectionAsync(int projectId, CancellationToken ct);
    Task<SurveyResponseContext?> GetResponseContextAsync(Guid token, string? normalizedEmail, CancellationToken ct);
    void AddResponse(SurveyResponse response);

    /// <summary>
    /// Traduz a falha de gravação sem obrigar o Application a conhecer EF ou
    /// Npgsql: só a Infrastructure sabe quais índices únicos representam uma
    /// resposta repetida.
    /// </summary>
    bool IsDuplicateResponseException(Exception exception);
}
