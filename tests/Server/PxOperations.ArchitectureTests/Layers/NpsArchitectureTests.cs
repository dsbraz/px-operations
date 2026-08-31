using System.Reflection;
using System.Text.Json;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Nps;

namespace PxOperations.ArchitectureTests.Layers;

public sealed class NpsArchitectureTests
{
    [Fact]
    public void Nps_api_feature_should_not_import_domain()
    {
        var files = ReadFiles("src/Server/PxOperations.Api/Features/Nps");

        Assert.DoesNotContain(files, file =>
            !file.Path.EndsWith("ApiExceptionHandler.cs", StringComparison.Ordinal) &&
            file.Content.Contains("using PxOperations.Domain", StringComparison.Ordinal));
    }

    [Fact]
    public void Nps_gets_should_depend_directly_on_queries_without_delegate_only_use_cases()
    {
        var files = ReadFiles("src/Server/PxOperations.Application/Features/Nps/UseCases");
        var controller = File.ReadAllText(Path.Combine(Root(), "src/Server/PxOperations.Api/Features/Nps/NpsController.cs"));

        Assert.DoesNotContain(files, file =>
            file.Path.Contains("GetNps", StringComparison.Ordinal) ||
            file.Path.Contains("ListNps", StringComparison.Ordinal));
        Assert.Contains("INpsQueries", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_repository_should_not_return_application_views()
    {
        var returnTypes = typeof(INpsRepository)
            .GetMethods()
            .Select(method => method.ReturnType.ToString())
            .ToArray();

        Assert.DoesNotContain(returnTypes, type => type.Contains("View", StringComparison.Ordinal));
    }

    [Fact]
    public void Commands_should_return_identifiers_instead_of_aggregates_or_views()
    {
        var commandFiles = ReadFiles("src/Server/PxOperations.Application/Features/Nps/UseCases");

        Assert.DoesNotContain(commandFiles, file =>
            file.Content.Contains("Task<Nps", StringComparison.Ordinal) ||
            file.Content.Contains("Task<Contact", StringComparison.Ordinal) ||
            file.Content.Contains("Task<Dispatch", StringComparison.Ordinal) ||
            file.Content.Contains("Task<SurveyResponse", StringComparison.Ordinal));
    }

    [Fact]
    public void Nps_aggregates_should_not_expose_navigation_to_another_aggregate()
    {
        var navigationTypes = new[]
        {
            typeof(Contact),
            typeof(SurveyResponse),
            typeof(DispatchTarget)
        };

        Assert.DoesNotContain(navigationTypes.SelectMany(type => type.GetProperties()), property =>
            property.PropertyType.Namespace?.StartsWith("PxOperations.Domain.Projects", StringComparison.Ordinal) == true ||
            property.PropertyType == typeof(Contact) ||
            property.PropertyType == typeof(SurveyResponse));
    }

    [Fact]
    public void Nps_openapi_should_publish_canonical_lowercase_filter_names()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(Root(), "specs/openapi/PxOperations.Api.json")));
        var parameters = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/nps/dashboard")
            .GetProperty("get")
            .GetProperty("parameters")
            .EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString()!)
            .ToArray();

        Assert.Equal(
            ["search", "client", "dc", "projectType", "deliveryManager", "status", "format", "classification", "from", "to", "includeWaived", "projectId"],
            parameters);
    }

    private static IReadOnlyList<SourceFile> ReadFiles(string relativeDirectory)
    {
        var directory = Path.Combine(Root(), relativeDirectory);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Select(path => new SourceFile(path, File.ReadAllText(path)))
            .ToArray();
    }

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PX-Operations.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed record SourceFile(string Path, string Content);
}
