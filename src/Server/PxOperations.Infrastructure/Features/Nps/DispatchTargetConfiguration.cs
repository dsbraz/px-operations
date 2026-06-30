using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Nps;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class DispatchTargetConfiguration : IEntityTypeConfiguration<DispatchTarget>
{
    public void Configure(EntityTypeBuilder<DispatchTarget> builder)
    {
        builder.ToTable("nps_dispatch_targets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(t => t.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(t => t.DispatchId).HasColumnName("dispatch_id").IsRequired();
        builder.Property(t => t.ContactId).HasColumnName("contact_id");
        builder.Property(t => t.Token).HasColumnName("token").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne(t => t.Project)
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Contact)
            .WithMany()
            .HasForeignKey(t => t.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Responses)
            .WithOne(r => r.Target)
            .HasForeignKey(r => r.TargetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => new { t.DispatchId, t.ContactId }).IsUnique().HasFilter("contact_id IS NOT NULL");
    }
}
