using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Abstractions;
using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Nps;
using PxOperations.Domain.ProjectHealth;
using PxOperations.Domain.Milestones;
using PxOperations.Domain.Projects;

namespace PxOperations.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Domain.ProjectHealth.ProjectHealth> ProjectHealth => Set<Domain.ProjectHealth.ProjectHealth>();
    public DbSet<Contact> NpsContacts => Set<Contact>();
    public DbSet<Dispatch> NpsDispatches => Set<Dispatch>();
    public DbSet<DispatchTarget> NpsDispatchTargets => Set<DispatchTarget>();
    public DbSet<SurveyResponse> NpsSurveyResponses => Set<SurveyResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
