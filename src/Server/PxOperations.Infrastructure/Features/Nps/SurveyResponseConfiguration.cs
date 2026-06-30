using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Nps;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("nps_survey_responses");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(r => r.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(r => r.DispatchId).HasColumnName("dispatch_id").IsRequired();
        builder.Property(r => r.TargetId).HasColumnName("target_id").IsRequired();
        builder.Property(r => r.ContactId).HasColumnName("contact_id");
        builder.Property(r => r.Score).HasColumnName("score").IsRequired();
        builder.Property(r => r.Classification).HasColumnName("classification").IsRequired();
        builder.Property(r => r.Scope).HasColumnName("scope");
        builder.Property(r => r.Schedule).HasColumnName("schedule");
        builder.Property(r => r.Quality).HasColumnName("quality");
        builder.Property(r => r.Communication).HasColumnName("communication");
        builder.Property(r => r.Tags).HasColumnName("tags").HasMaxLength(500);
        builder.Property(r => r.Comment).HasColumnName("comment").HasMaxLength(2000);
        builder.Property(r => r.RespondentName).HasColumnName("respondent_name").HasMaxLength(200);
        builder.Property(r => r.RespondentEmail).HasColumnName("respondent_email").HasMaxLength(320);
        builder.Property(r => r.SubmittedAt).HasColumnName("submitted_at").IsRequired();

        builder.HasOne(r => r.Project)
            .WithMany()
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Dispatch)
            .WithMany()
            .HasForeignKey(r => r.DispatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Contact)
            .WithMany()
            .HasForeignKey(r => r.ContactId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => new { r.ProjectId, r.SubmittedAt });
        builder.HasIndex(r => new { r.DispatchId, r.SubmittedAt });
        builder.HasIndex(r => r.TargetId).IsUnique();
    }
}
