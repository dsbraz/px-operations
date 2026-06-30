using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Nps.Rules;
using PxOperations.Domain.Projects;
using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Nps;

public sealed class Contact : AggregateRoot<int>
{
    private Contact() : base(default) { }

    public int ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Role { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public Project Project { get; private set; } = default!;

    public static Contact Create(int projectId, string name, string email, string? role, DateTimeOffset now)
    {
        CheckRules(name, email);

        return new Contact
        {
            ProjectId = projectId,
            Name = name.Trim(),
            Email = email.Trim(),
            Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim(),
            CreatedAt = now
        };
    }

    public void Update(string name, string email, string? role)
    {
        CheckRules(name, email);

        Name = name.Trim();
        Email = email.Trim();
        Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
    }

    public void Archive(DateTimeOffset now)
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
        ArchivedAt = now;
    }

    private static void CheckRules(string name, string email)
    {
        RuleChecker.Check(new ContactNameMustNotBeEmptyRule(name));
        RuleChecker.Check(new ContactEmailMustNotBeEmptyRule(email));
    }
}
