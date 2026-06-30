using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Projects;

namespace PxOperations.Domain.Nps;

public sealed class DispatchTarget : Entity<int>
{
    private DispatchTarget() : base(default) { }

    public int ProjectId { get; private set; }
    public int DispatchId { get; private set; }
    public int? ContactId { get; private set; }
    public Guid Token { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Project Project { get; private set; } = default!;
    public Dispatch Dispatch { get; private set; } = default!;
    public Contact? Contact { get; private set; }
    public ICollection<SurveyResponse> Responses { get; } = [];

    public bool IsGeneric => ContactId is null;

    public static DispatchTarget CreateContact(int projectId, int dispatchId, int contactId, Guid token, DateTimeOffset now)
    {
        return new DispatchTarget
        {
            ProjectId = projectId,
            DispatchId = dispatchId,
            ContactId = contactId,
            Token = token,
            CreatedAt = now
        };
    }

    public static DispatchTarget CreateGeneric(int projectId, int dispatchId, Guid token, DateTimeOffset now)
    {
        return new DispatchTarget
        {
            ProjectId = projectId,
            DispatchId = dispatchId,
            Token = token,
            CreatedAt = now
        };
    }
}
