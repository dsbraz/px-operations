using Microsoft.AspNetCore.Mvc;
using PxOperations.Application.Features.Team;

namespace PxOperations.Api.Features.Team;

[ApiController]
[Route("api/team-members")]
public sealed class TeamMembersController : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<TeamMemberView>> ListTeamMembers() => Ok(TeamMemberViews.List());
}
