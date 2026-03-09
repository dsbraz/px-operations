using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Abstractions;

namespace PxOperations.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
}
