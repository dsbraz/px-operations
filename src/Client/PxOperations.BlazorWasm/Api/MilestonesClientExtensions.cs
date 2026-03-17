using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Api;

public partial class MilestonesClient
{
    public Task<ICollection<MilestoneResponse>> ListAsync(
        string? search,
        string? dc,
        string? type,
        int? projectId,
        string? from,
        string? to,
        CancellationToken cancellationToken = default)
        => List2Async(search, dc, type, projectId, from, to, cancellationToken);

    public Task<MilestoneResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => GetById2Async(id, cancellationToken);

    public Task<MilestoneResponse> CreateAsync(CreateMilestoneRequest body, CancellationToken cancellationToken = default)
        => Create2Async(body, cancellationToken);

    public Task<MilestoneResponse> UpdateAsync(int id, UpdateMilestoneRequest body, CancellationToken cancellationToken = default)
        => Update2Async(id, body, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => Delete2Async(id, cancellationToken);
}
