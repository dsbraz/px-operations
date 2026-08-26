using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Nps;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class CollectionWaiverConfiguration : IEntityTypeConfiguration<CollectionWaiver>
{
    public void Configure(EntityTypeBuilder<CollectionWaiver> builder)
    {
        builder.ToTable("nps_collection_waivers");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id");
        builder.Property(w => w.ProjectId).HasColumnName("project_id");
        builder.Property(w => w.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(w => w.DismissedAt).HasColumnName("dismissed_at");
        builder.Property(w => w.ReactivatedAt).HasColumnName("reactivated_at");

        builder.HasOne(w => w.Project)
            .WithMany()
            .HasForeignKey(w => w.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Um projeto tem no máximo uma dispensa ativa. Parcial porque as
        // reativadas ficam no histórico e podem se repetir.
        builder.HasIndex(w => w.ProjectId)
            .IsUnique()
            .HasFilter("reactivated_at IS NULL");
    }
}
