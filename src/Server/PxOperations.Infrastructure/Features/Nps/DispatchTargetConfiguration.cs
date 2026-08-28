using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Nps;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class DispatchTargetConfiguration : IEntityTypeConfiguration<DispatchTarget>
{
    public void Configure(EntityTypeBuilder<DispatchTarget> builder)
    {
        builder.ToTable("nps_dispatch_targets");
        builder.HasKey(target => target.Id);
        builder.Property(target => target.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(target => target.DispatchId).HasColumnName("dispatch_id").IsRequired();
        builder.Property(target => target.ContactId).HasColumnName("contact_id");
        builder.Property(target => target.Token).HasColumnName("token").IsRequired();
        builder.Property(target => target.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Ignore(target => target.IsGeneric);
        builder.HasOne<Contact>().WithMany().HasForeignKey(target => target.ContactId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(target => target.Token).IsUnique();
        builder.HasIndex(target => new { target.DispatchId, target.ContactId }).IsUnique().HasFilter("contact_id IS NOT NULL");
    }
}
