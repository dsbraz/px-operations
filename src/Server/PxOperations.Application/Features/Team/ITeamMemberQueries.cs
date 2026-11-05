namespace PxOperations.Application.Features.Team;

public interface ITeamMemberQueries
{
    IReadOnlyList<TeamMemberView> ListTeamMembers();
}
