using PxOperations.Ui.Components.Navigation;

namespace PxOperations.Ui.Tests.Architecture;

public sealed class UiDependencyTests
{
    [Fact]
    public void Ui_assembly_should_not_reference_client_or_server_projects()
    {
        var references = typeof(BrqAppShell).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, name =>
            name.StartsWith("PxOperations.BlazorWasm", StringComparison.Ordinal)
            || name.StartsWith("PxOperations.Api", StringComparison.Ordinal)
            || name.StartsWith("PxOperations.Application", StringComparison.Ordinal)
            || name.StartsWith("PxOperations.Domain", StringComparison.Ordinal)
            || name.StartsWith("PxOperations.Infrastructure", StringComparison.Ordinal));
    }
}
