using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Nps;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class DispatchConfiguration : IEntityTypeConfiguration<Dispatch>
{
    public void Configure(EntityTypeBuilder<Dispatch> builder)
    {
        builder.ToTable("nps_dispatches");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(d => d.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(d => d.PeriodStart).HasColumnName("period_start").IsRequired();
        builder.Property(d => d.PeriodEnd).HasColumnName("period_end").IsRequired();
        builder.Property(d => d.Format).HasColumnName("format").IsRequired();
        builder.Property(d => d.Language).HasColumnName("language").IsRequired();
        builder.Property(d => d.Status).HasColumnName("status").IsRequired();
        builder.Property(d => d.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(d => d.ClosedAt).HasColumnName("closed_at");

        builder.HasOne(d => d.Project)
            .WithMany()
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Targets)
            .WithOne(t => t.Dispatch)
            .HasForeignKey(t => t.DispatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(d => d.Targets).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(d => new { d.ProjectId, d.Status });
    }
}
