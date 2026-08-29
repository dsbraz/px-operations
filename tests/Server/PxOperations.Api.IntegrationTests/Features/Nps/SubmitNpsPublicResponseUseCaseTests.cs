using PxOperations.Application.Abstractions;
using PxOperations.Application.Features.Nps;
using PxOperations.Application.Features.Nps.UseCases;
using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;

namespace PxOperations.Api.IntegrationTests.Features.Nps;

public sealed class SubmitNpsPublicResponseUseCaseTests
{
    /// <summary>
    /// A checagem de disponibilidade lê o banco antes de gravar, então duas
    /// respostas simultâneas no mesmo link passam as duas pela checagem e a
    /// segunda só é barrada pelo índice único. Isso é conflito de estado, não
    /// erro do servidor.
    /// </summary>
    [Fact]
    public async Task A_duplicate_insert_should_surface_as_a_state_conflict()
    {
        var repository = new FakeNpsRepository();
        var useCase = new SubmitNpsPublicResponseUseCase(
            repository,
            new FailingUnitOfWork(),
            TimeProvider.System);

        await Assert.ThrowsAsync<BusinessStateConflictException>(() => useCase.ExecuteAsync(
            new SubmitNpsPublicResponseCommand(Guid.NewGuid(), 9, null, null, null, null, null, null, "person@example.com"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Any_other_save_failure_should_keep_its_own_type()
    {
        var repository = new FakeNpsRepository { RecognizesDuplicate = false };
        var useCase = new SubmitNpsPublicResponseUseCase(
            repository,
            new FailingUnitOfWork(),
            TimeProvider.System);

        await Assert.ThrowsAsync<SaveFailure>(() => useCase.ExecuteAsync(
            new SubmitNpsPublicResponseCommand(Guid.NewGuid(), 9, null, null, null, null, null, null, "person@example.com"),
            CancellationToken.None));
    }

    private sealed class SaveFailure : Exception;

    private sealed class FailingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new SaveFailure();
    }

    private sealed class FakeNpsRepository : INpsRepository
    {
        public bool RecognizesDuplicate { get; init; } = true;

        public bool IsDuplicateResponseException(Exception exception)
            => RecognizesDuplicate && exception is SaveFailure;

        public Task<SurveyResponseContext?> GetResponseContextAsync(
            Guid token,
            string? normalizedEmail,
            CancellationToken ct)
            => Task.FromResult<SurveyResponseContext?>(new SurveyResponseContext(
                ProjectId: 1,
                DispatchId: 2,
                TargetId: 3,
                ContactId: null,
                Format: NpsFormFormat.Simplified,
                DispatchStatus: NpsDispatchStatus.Open,
                ExpiresAt: TimeProvider.System.GetUtcNow().AddDays(10),
                IsWaived: false,
                IsTargetUsed: false,
                HasDuplicateEmail: false));

        public void AddResponse(SurveyResponse response)
        {
        }

        public Task<bool> ProjectExistsAsync(int projectId, CancellationToken ct) => Task.FromResult(true);

        public Task<bool> ContactBelongsToProjectAsync(int projectId, int contactId, CancellationToken ct)
            => Task.FromResult(true);

        public Task<Contact?> GetContactAsync(int id, CancellationToken ct) => Task.FromResult<Contact?>(null);

        public void AddContact(Contact contact)
        {
        }

        public Task<NpsCollection?> GetCollectionAsync(int projectId, CancellationToken ct)
            => Task.FromResult<NpsCollection?>(null);

        public Task<NpsCollection> GetOrCreateCollectionAsync(int projectId, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
