using Microsoft.EntityFrameworkCore;
using Npgsql;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Nps;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class NpsRepository(AppDbContext dbContext) : INpsRepository
{
    public Task<bool> ProjectExistsAsync(int projectId, CancellationToken ct)
        => dbContext.Projects.AnyAsync(project => project.Id == projectId, ct);

    public Task<bool> ContactBelongsToProjectAsync(int projectId, int contactId, CancellationToken ct)
        => dbContext.NpsContacts.AnyAsync(
            contact => contact.Id == contactId && contact.ProjectId == projectId && !contact.IsArchived,
            ct);

    public Task<Contact?> GetContactAsync(int id, CancellationToken ct)
        => dbContext.NpsContacts.FirstOrDefaultAsync(contact => contact.Id == id, ct);

    public void AddContact(Contact contact) => dbContext.NpsContacts.Add(contact);

    public Task<NpsCollection?> GetCollectionAsync(int projectId, CancellationToken ct)
        => dbContext.NpsCollections
            .Include(collection => collection.Dispatches)
            .ThenInclude(dispatch => dispatch.Targets)
            .FirstOrDefaultAsync(collection => collection.ProjectId == projectId, ct);

    public async Task<NpsCollection> GetOrCreateCollectionAsync(int projectId, CancellationToken ct)
    {
        var existing = await GetCollectionAsync(projectId, ct);
        if (existing is not null)
        {
            return existing;
        }

        // Dois pedidos podem chegar aqui juntos e os dois verem a coleção
        // ausente. O índice único de project_id decide quem cria; quem perde
        // solta a própria tentativa do rastreador e relê o que o outro gravou.
        var created = NpsCollection.Create(projectId);
        dbContext.NpsCollections.Add(created);
        try
        {
            await dbContext.SaveChangesAsync(ct);
            return created;
        }
        catch (DbUpdateException exception) when (IsDuplicateCollectionException(exception))
        {
            dbContext.Entry(created).State = EntityState.Detached;
        }

        return await GetCollectionAsync(projectId, ct)
            ?? throw new InvalidOperationException("The NPS collection could not be persisted.");
    }

    private static bool IsDuplicateCollectionException(DbUpdateException exception)
        => exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_nps_collections_project_id"
        };

    public async Task<SurveyResponseContext?> GetResponseContextAsync(
        Guid token,
        string? normalizedEmail,
        CancellationToken ct)
    {
        var target = await dbContext.NpsDispatchTargets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Token == token, ct);
        if (target is null)
        {
            return null;
        }

        var dispatch = await dbContext.NpsDispatches
            .AsNoTracking()
            .FirstAsync(item => item.Id == target.DispatchId, ct);
        var collection = await dbContext.NpsCollections
            .AsNoTracking()
            .FirstAsync(item => item.Id == dispatch.CollectionId, ct);
        var isTargetUsed = await dbContext.NpsSurveyResponses
            .AnyAsync(response => response.TargetId == target.Id, ct);
        var hasDuplicateEmail = normalizedEmail is not null && await dbContext.NpsSurveyResponses
            .AnyAsync(
                response => response.TargetId == target.Id &&
                    response.NormalizedRespondentEmail == normalizedEmail,
                ct);

        return new SurveyResponseContext(
            collection.ProjectId,
            dispatch.Id,
            target.Id,
            target.ContactId,
            dispatch.Format,
            dispatch.Status,
            dispatch.ExpiresAt,
            collection.IsWaived,
            isTargetUsed,
            hasDuplicateEmail);
    }

    public void AddResponse(SurveyResponse response) => dbContext.NpsSurveyResponses.Add(response);

    // A disponibilidade é checada lendo o banco antes de gravar, então duas
    // respostas simultâneas no mesmo link passam as duas pela checagem e a
    // segunda só é barrada pelo índice único. Quem perde a corrida está em
    // conflito de estado, não diante de um erro do servidor.
    // O disparo é fechado e reaberto na mesma transação, mas dois operadores
    // gerando link ao mesmo tempo leem a coleção antes de o outro gravar: cada
    // um abre o seu, e o índice filtrado por status barra o segundo.
    public bool IsDuplicateDispatchException(Exception exception)
        => exception is DbUpdateException
        {
            InnerException: PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_nps_dispatches_collection_id_format"
            }
        };

    public bool IsDuplicateResponseException(Exception exception)
        => exception is DbUpdateException
        {
            InnerException: PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_nps_survey_responses_target_id"
                    or "IX_nps_survey_responses_target_id_normalized_respondent_email"
            }
        };
}
