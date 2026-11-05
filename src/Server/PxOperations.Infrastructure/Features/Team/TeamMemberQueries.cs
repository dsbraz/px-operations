using PxOperations.Application.Features.Team;
using PxOperations.Domain.Team;

namespace PxOperations.Infrastructure.Features.Team;

public sealed class TeamMemberQueries : ITeamMemberQueries
{
    public IReadOnlyList<TeamMemberView> ListTeamMembers()
        => TeamRoster.Members
            .Select(member => new TeamMemberView(member.Name, member.CanBeDeliveryManager))
            .ToArray();
}
