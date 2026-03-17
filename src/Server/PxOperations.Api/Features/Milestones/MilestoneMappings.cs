using PxOperations.Api.Features.Milestones.Contracts;
using PxOperations.Application.Features.Milestones;
using PxOperations.Domain.Milestones;
using PxOperations.Domain.Projects;
using ProjectMappings = PxOperations.Api.Features.Projects.ProjectMappings;

namespace PxOperations.Api.Features.Milestones;

public static class MilestoneMappings
{
    public static MilestoneResponse ToResponse(MilestoneView milestone)
    {
        return new MilestoneResponse(
            milestone.Id,
            milestone.ProjectId,
            milestone.ProjectName,
            milestone.ProjectClient,
            milestone.ProjectDc,
            milestone.Type,
            milestone.Title,
            milestone.Date,
            milestone.Time,
            milestone.Notes);
    }

    public static MilestoneType ParseMilestoneType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "apresentação sponsor" or "apresentacao sponsor" or "sponsor_presentation" => MilestoneType.SponsorPresentation,
            "entrega final" or "final_delivery" => MilestoneType.FinalDelivery,
            "presencial com cliente" or "client_onsite" => MilestoneType.ClientOnsite,
            "kickoff" => MilestoneType.Kickoff,
            "outros" or "other" => MilestoneType.Other,
            _ => throw new ArgumentException($"Invalid milestone type: {value}")
        };
    }

    public static string FormatMilestoneType(MilestoneType type) => type switch
    {
        MilestoneType.SponsorPresentation => "Apresentação Sponsor",
        MilestoneType.FinalDelivery => "Entrega Final",
        MilestoneType.ClientOnsite => "Presencial com Cliente",
        MilestoneType.Kickoff => "Kickoff",
        _ => "Outros"
    };

    public static DeliveryCenter? ParseDeliveryCenterOrNull(string? value)
        => value is null ? null : ProjectMappings.ParseDeliveryCenter(value);
}
