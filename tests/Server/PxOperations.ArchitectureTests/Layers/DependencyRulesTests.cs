using System.Xml.Linq;

namespace PxOperations.ArchitectureTests.Layers;

public sealed class DependencyRulesTests
{
    [Fact]
    public void Domain_should_not_reference_other_solution_projects()
    {
        var references = GetProjectReferenceNames("src/Server/PxOperations.Domain/PxOperations.Domain.csproj");

        Assert.DoesNotContain("PxOperations.Application", references);
        Assert.DoesNotContain("PxOperations.Infrastructure", references);
        Assert.DoesNotContain("PxOperations.Api", references);
        Assert.DoesNotContain("PxOperations.BlazorWasm", references);
    }

    [Fact]
    public void Application_should_reference_only_domain_within_solution()
    {
        var references = GetProjectReferenceNames("src/Server/PxOperations.Application/PxOperations.Application.csproj");

        Assert.Contains("PxOperations.Domain", references);
        Assert.DoesNotContain("PxOperations.Infrastructure", references);
        Assert.DoesNotContain("PxOperations.Api", references);
        Assert.DoesNotContain("PxOperations.BlazorWasm", references);
    }

    [Fact]
    public void Infrastructure_should_reference_application_and_domain()
    {
        var references = GetProjectReferenceNames("src/Server/PxOperations.Infrastructure/PxOperations.Infrastructure.csproj");

        Assert.Contains("PxOperations.Application", references);
        Assert.Contains("PxOperations.Domain", references);
        Assert.DoesNotContain("PxOperations.Api", references);
        Assert.DoesNotContain("PxOperations.BlazorWasm", references);
    }

    [Fact]
    public void Api_should_not_reference_domain_directly()
    {
        var references = GetProjectReferenceNames("src/Server/PxOperations.Api/PxOperations.Api.csproj");

        Assert.Contains("PxOperations.Application", references);
        Assert.Contains("PxOperations.Infrastructure", references);
        Assert.DoesNotContain("PxOperations.Domain", references);
        Assert.DoesNotContain("PxOperations.BlazorWasm", references);
    }

    [Fact]
    public void Blazor_client_should_not_reference_server_projects()
    {
        var references = GetProjectReferenceNames("src/Client/PxOperations.BlazorWasm/PxOperations.BlazorWasm.csproj");

        Assert.DoesNotContain("PxOperations.Api", references);
        Assert.DoesNotContain("PxOperations.Application", references);
        Assert.DoesNotContain("PxOperations.Domain", references);
        Assert.DoesNotContain("PxOperations.Infrastructure", references);
    }

    private static HashSet<string> GetProjectReferenceNames(string relativeProjectPath)
    {
        var rootDirectory = FindRepositoryRoot();
        var projectPath = Path.Combine(rootDirectory, relativeProjectPath);
        var document = XDocument.Load(projectPath);

        return document.Descendants("ProjectReference")
            .Select(static element => element.Attribute("Include")?.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar))
            .Select(static value => Path.GetFileNameWithoutExtension(value))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var currentDirectory = AppContext.BaseDirectory;
        var directory = new DirectoryInfo(currentDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "px-operations.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Repository root not found.");
    }
}
