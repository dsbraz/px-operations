using System.Text.Json;
using System.Text.RegularExpressions;

namespace PxOperations.BlazorWasm.Tests.Hosting;

/// <summary>
/// O tema só sobrevive a um reload se algo ler a preferência gravada antes do
/// primeiro paint. Esse "algo" é um script solto no head — sem referência em
/// código C#, ninguém percebe quando ele some do index.html.
/// </summary>
public sealed partial class ThemeBootstrapTests
{
    private const string ThemeInitPath = "_content/PxOperations.Ui/js/theme-init.js";

    [Fact]
    public void Index_should_load_the_theme_init_script_inside_the_head()
    {
        var html = File.ReadAllText(ResolveAsset("index.html"));

        var scriptIndex = html.IndexOf(ThemeInitPath, StringComparison.Ordinal);
        Assert.True(
            scriptIndex >= 0,
            "index.html precisa carregar theme-init.js: sem ele a preferência de tema é gravada e nunca lida.");

        var headEnd = html.IndexOf("</head>", StringComparison.Ordinal);
        Assert.True(
            headEnd >= 0 && scriptIndex < headEnd,
            "theme-init.js precisa estar no head, antes do primeiro paint.");
    }

    [Fact]
    public void Theme_init_script_should_run_before_the_first_paint()
    {
        var html = File.ReadAllText(ResolveAsset("index.html"));

        var tag = ScriptTagRegex()
            .Matches(html)
            .Select(match => match.Value)
            .Single(value => value.Contains(ThemeInitPath, StringComparison.Ordinal));

        Assert.DoesNotContain("type=\"module\"", tag, StringComparison.Ordinal);
        Assert.DoesNotContain("defer", tag, StringComparison.Ordinal);
        Assert.DoesNotContain("async", tag, StringComparison.Ordinal);
    }

    [Fact]
    public void Theme_init_script_should_resolve_to_a_published_asset()
    {
        Assert.True(File.Exists(ResolveAsset("_content", "PxOperations.Ui", "js", "theme-init.js")));
    }

    private static string ResolveAsset(params string[] segments)
    {
        var manifestPath = Path.Combine(
            AppContext.BaseDirectory,
            "PxOperations.BlazorWasm.staticwebassets.runtime.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = manifest.RootElement;

        var node = root.GetProperty("Root");
        foreach (var segment in segments)
            node = node.GetProperty("Children").GetProperty(segment);

        var asset = node.GetProperty("Asset");
        var contentRoot = root
            .GetProperty("ContentRoots")[asset.GetProperty("ContentRootIndex").GetInt32()]
            .GetString()!;
        return Path.Combine(contentRoot, asset.GetProperty("SubPath").GetString()!);
    }

    [GeneratedRegex("<script[^>]*>", RegexOptions.CultureInvariant)]
    private static partial Regex ScriptTagRegex();
}
