using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Projects;

namespace PxOperations.Infrastructure.Features.Projects;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Dc)
            .HasColumnName("dc")
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Client)
            .HasColumnName("client")
            .HasMaxLength(200);

        builder.Property(p => p.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(p => p.StartDate)
            .HasColumnName("start_date");

        builder.Property(p => p.EndDate)
            .HasColumnName("end_date");

        builder.Property(p => p.DeliveryManager)
            .HasColumnName("delivery_manager")
            .HasMaxLength(200);

        builder.Property(p => p.Renewal)
            .HasColumnName("renewal");

        builder.Property(p => p.RenewalObservation)
            .HasColumnName("renewal_observation")
            .HasMaxLength(500);
    }
}
