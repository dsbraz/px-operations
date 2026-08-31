using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Projects;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class NpsCollectionConfiguration : IEntityTypeConfiguration<NpsCollection>
{
    public void Configure(EntityTypeBuilder<NpsCollection> builder)
    {
        builder.ToTable("nps_collections");
        builder.HasKey(collection => collection.Id);
        builder.Property(collection => collection.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(collection => collection.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(collection => collection.WaiverReason).HasColumnName("waiver_reason").HasMaxLength(500);
        builder.Property(collection => collection.WaivedAt).HasColumnName("waived_at");
        builder.Ignore(collection => collection.IsWaived);
        builder.HasOne<Project>().WithOne().HasForeignKey<NpsCollection>(collection => collection.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(collection => collection.Dispatches).WithOne().HasForeignKey(dispatch => dispatch.CollectionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(collection => collection.Dispatches).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(collection => collection.ProjectId).IsUnique();
    }
}
