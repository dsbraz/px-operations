using PxOperations.Application.Abstractions;

namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class CloseNpsDispatchUseCase(INpsRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<NpsDispatchDetailView?> ExecuteAsync(int id, CancellationToken ct)
    {
        var dispatch = await repository.GetDispatchEntityAsync(id, ct);
        if (dispatch is null)
        {
            return null;
        }

        dispatch.Close(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return await repository.GetDispatchAsync(id, ct);
    }
}
