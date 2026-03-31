using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Abstractions;
using PxOperations.Domain.Abstractions;
using PxOperations.Domain.HealthChecks;
using PxOperations.Domain.Milestones;
using PxOperations.Domain.Projects;

namespace PxOperations.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<HealthCheck> HealthChecks => Set<HealthCheck>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
