using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Nps;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class DispatchConfiguration : IEntityTypeConfiguration<Dispatch>
{
    public void Configure(EntityTypeBuilder<Dispatch> builder)
    {
        builder.ToTable("nps_dispatches");
        builder.HasKey(dispatch => dispatch.Id);
        builder.Property(dispatch => dispatch.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(dispatch => dispatch.CollectionId).HasColumnName("collection_id").IsRequired();
        builder.Property(dispatch => dispatch.Format).HasColumnName("format").IsRequired();
        builder.Property(dispatch => dispatch.Language).HasColumnName("language").IsRequired();
        builder.Property(dispatch => dispatch.Status).HasColumnName("status").IsRequired();
        builder.Property(dispatch => dispatch.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(dispatch => dispatch.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(dispatch => dispatch.ClosedAt).HasColumnName("closed_at");
        builder.Ignore(dispatch => dispatch.IsOpen);
        builder.HasMany(dispatch => dispatch.Targets).WithOne().HasForeignKey(target => target.DispatchId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(dispatch => dispatch.Targets).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(dispatch => new { dispatch.CollectionId, dispatch.Format })
            .IsUnique()
            .HasFilter("status = 0");
    }
}
