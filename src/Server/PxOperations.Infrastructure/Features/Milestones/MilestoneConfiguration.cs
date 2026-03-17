using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Milestones;

namespace PxOperations.Infrastructure.Features.Milestones;

public sealed class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> builder)
    {
        builder.ToTable("milestones");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.ProjectId).HasColumnName("project_id");
        builder.Property(m => m.Type).HasColumnName("type");
        builder.Property(m => m.Title).HasColumnName("title").HasMaxLength(200);
        builder.Property(m => m.Date).HasColumnName("date");
        builder.Property(m => m.Time).HasColumnName("time");
        builder.Property(m => m.Notes).HasColumnName("notes").HasMaxLength(1000);

        builder.HasOne(m => m.Project)
            .WithMany(p => p.Milestones)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
