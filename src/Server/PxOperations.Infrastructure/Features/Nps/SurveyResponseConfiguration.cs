using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Projects;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("nps_survey_responses", table =>
        {
            table.HasCheckConstraint("CK_nps_survey_responses_score", "score BETWEEN 1 AND 10");
            table.HasCheckConstraint("CK_nps_survey_responses_quality", "quality IS NULL OR quality BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_nps_survey_responses_schedule", "schedule IS NULL OR schedule BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_nps_survey_responses_communication", "communication IS NULL OR communication BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_nps_survey_responses_business_value", "business_value IS NULL OR business_value BETWEEN 1 AND 5");
        });

        builder.HasKey(response => response.Id);
        builder.Property(response => response.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(response => response.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(response => response.DispatchId).HasColumnName("dispatch_id").IsRequired();
        builder.Property(response => response.TargetId).HasColumnName("target_id").IsRequired();
        builder.Property(response => response.ContactId).HasColumnName("contact_id");
        builder.Property(response => response.Format).HasColumnName("format").IsRequired();
        builder.Property(response => response.Score).HasColumnName("score").IsRequired();
        builder.Property(response => response.Classification).HasColumnName("classification").IsRequired();
        builder.Property(response => response.Quality).HasColumnName("quality");
        builder.Property(response => response.Schedule).HasColumnName("schedule");
        builder.Property(response => response.Communication).HasColumnName("communication");
        builder.Property(response => response.BusinessValue).HasColumnName("business_value");
        builder.Property(response => response.Comment).HasColumnName("comment").HasMaxLength(2000);
        builder.Property(response => response.RespondentName).HasColumnName("respondent_name").HasMaxLength(200);
        builder.Property(response => response.RespondentEmail).HasColumnName("respondent_email").HasMaxLength(320);
        builder.Property(response => response.NormalizedRespondentEmail).HasColumnName("normalized_respondent_email").HasMaxLength(320);
        builder.Property(response => response.SubmittedAt).HasColumnName("submitted_at").IsRequired();
        builder.HasOne<Project>().WithMany().HasForeignKey(response => response.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Dispatch>().WithMany().HasForeignKey(response => response.DispatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<DispatchTarget>().WithMany().HasForeignKey(response => response.TargetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Contact>().WithMany().HasForeignKey(response => response.ContactId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(response => new { response.ProjectId, response.SubmittedAt });
        builder.HasIndex(response => new { response.DispatchId, response.SubmittedAt });
        builder.HasIndex(response => response.TargetId)
            .IsUnique()
            .HasFilter("contact_id IS NOT NULL");
        builder.HasIndex(response => new { response.TargetId, response.NormalizedRespondentEmail })
            .IsUnique()
            .HasFilter("contact_id IS NULL AND normalized_respondent_email IS NOT NULL");
    }
}
