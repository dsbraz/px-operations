using System.Xml.Linq;

namespace PxOperations.Ui.Tests.Architecture;

/// <summary>
/// A RCL é a fundação visual compartilhada: ela não pode conhecer nenhuma
/// outra parte da solution. A verificação lê o csproj em vez de inspecionar
/// o assembly, porque <c>Assembly.GetReferencedAssemblies()</c> só enxerga
/// referências cujos tipos foram efetivamente usados — um ProjectReference
/// indevido mas ainda não consumido passaria despercebido.
/// </summary>
public sealed class UiDependencyTests
{
    private const string UiProjectPath = "src/Client/PxOperations.Ui/PxOperations.Ui.csproj";

    [Fact]
    public void Ui_project_should_not_reference_any_other_solution_project()
    {
        var references = GetProjectReferenceNames(UiProjectPath);

        Assert.Empty(references);
    }

    [Fact]
    public void Ui_project_should_not_depend_on_feature_or_server_namespaces()
    {
        var sources = Directory
            .EnumerateFiles(
                Path.Combine(FindRepositoryRoot(), "src", "Client", "PxOperations.Ui"),
                "*.*",
                SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal)
                || path.EndsWith(".razor", StringComparison.Ordinal));

        var offenders = sources
            .Where(path => File.ReadAllText(path)
                .Contains("PxOperations.BlazorWasm", StringComparison.Ordinal))
            .Select(path => Path.GetFileName(path))
            .ToArray();

        Assert.Empty(offenders);
    }

    private static HashSet<string> GetProjectReferenceNames(string relativeProjectPath)
    {
        var projectPath = Path.Combine(FindRepositoryRoot(), relativeProjectPath);
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
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "PX-Operations.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Repository root not found.");
    }
}
