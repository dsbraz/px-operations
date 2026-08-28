using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Projects;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("nps_contacts");
        builder.HasKey(contact => contact.Id);
        builder.Property(contact => contact.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(contact => contact.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(contact => contact.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(contact => contact.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(contact => contact.Role).HasColumnName("role").HasMaxLength(120);
        builder.Property(contact => contact.IsArchived).HasColumnName("is_archived").IsRequired();
        builder.Property(contact => contact.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(contact => contact.ArchivedAt).HasColumnName("archived_at");
        builder.HasOne<Project>().WithMany().HasForeignKey(contact => contact.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(contact => new { contact.ProjectId, contact.Email });
    }
}
