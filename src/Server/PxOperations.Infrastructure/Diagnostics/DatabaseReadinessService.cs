using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Diagnostics;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Infrastructure.Diagnostics;

public sealed class DatabaseReadinessService(AppDbContext dbContext) : IReadinessService
{
    public async Task<ReadinessStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? new ReadinessStatus(true, "Ready")
            : new ReadinessStatus(false, "Database unavailable");
    }
}
