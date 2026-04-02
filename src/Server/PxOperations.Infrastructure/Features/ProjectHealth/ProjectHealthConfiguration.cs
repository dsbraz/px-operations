using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.ProjectHealth;

namespace PxOperations.Infrastructure.Features.ProjectHealth;

public sealed class ProjectHealthConfiguration : IEntityTypeConfiguration<Domain.ProjectHealth.ProjectHealth>
{
    public void Configure(EntityTypeBuilder<Domain.ProjectHealth.ProjectHealth> builder)
    {
        builder.ToTable("project_health");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.ProjectId).HasColumnName("project_id");
        builder.Property(h => h.SubProject).HasColumnName("sub_project").HasMaxLength(200);
        builder.Property(h => h.Week).HasColumnName("week");
        builder.Property(h => h.ReporterEmail).HasColumnName("reporter_email").HasMaxLength(200);
        builder.Property(h => h.PracticesCount).HasColumnName("practices_count");
        builder.Property(h => h.Scope).HasColumnName("scope");
        builder.Property(h => h.Schedule).HasColumnName("schedule");
        builder.Property(h => h.Quality).HasColumnName("quality");
        builder.Property(h => h.Satisfaction).HasColumnName("satisfaction");
        builder.Property(h => h.ExpansionOpportunity).HasColumnName("expansion_opportunity");
        builder.Property(h => h.ExpansionComment).HasColumnName("expansion_comment").HasMaxLength(500);
        builder.Property(h => h.ActionPlanNeeded).HasColumnName("action_plan_needed");
        builder.Property(h => h.Highlights).HasColumnName("highlights").HasMaxLength(2000);
        builder.Property(h => h.Score).HasColumnName("score");

        builder.HasOne(h => h.Project)
            .WithMany()
            .HasForeignKey(h => h.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.ProjectId, h.Week });
    }
}
