using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PxOperations.Ui.Tests.Accessibility;

public sealed partial class ThemeTokenContrastTests
{
    private const double NormalTextMinimum = 4.5;
    private const double NonTextMinimum = 3;

    /// <summary>
    /// Trava os tokens herdados do painel de NPS, que é a referência visual da
    /// migração. A comparação é por valor do token, não por texto literal do
    /// arquivo: reindentar o CSS não pode quebrar o teste.
    /// </summary>
    [Theory]
    [InlineData("--color-black", "#121212")]
    [InlineData("--color-purple", "#7f2ec9")]
    [InlineData("--color-gray", "#626262")]
    [InlineData("--sidebar-width", "15.5rem")]
    [InlineData("--topbar-height", "3.75rem")]
    [InlineData("--control-h", "2.25rem")]
    [InlineData("--fs-stat", "1.625rem")]
    [InlineData("--radius-md", "8px")]
    public void Foundation_should_preserve_the_nps_reference_tokens(string token, string expected)
    {
        Assert.Equal(expected, ReadRootToken(token));
    }

    [Theory]
    [InlineData("--font-display", "Aspekta")]
    [InlineData("--font-body", "Inter")]
    [InlineData("--font-mono", "Geist Mono")]
    public void Foundation_should_lead_each_font_stack_with_the_reference_family(
        string token,
        string expectedFamily)
    {
        Assert.StartsWith($"\"{expectedFamily}\"", ReadRootToken(token), StringComparison.Ordinal);
    }

    /// <summary>
    /// Lê um token do primeiro bloco <c>:root</c>. Diferente de
    /// <see cref="ParseTheme"/>, aceita qualquer forma de valor — cor,
    /// comprimento ou stack de fonte.
    /// </summary>
    private static string ReadRootToken(string name)
    {
        var css = File.ReadAllText(FindFoundationCss());
        var blockStart = css.IndexOf(":root {", StringComparison.Ordinal);
        Assert.True(blockStart >= 0, "Bloco :root não encontrado.");

        var blockEnd = css.IndexOf('}', blockStart);
        Assert.True(blockEnd > blockStart, "Bloco :root está incompleto.");

        var match = Regex.Match(
            css[blockStart..blockEnd],
            $@"{Regex.Escape(name)}\s*:\s*(?<value>[^;]+);",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, $"Token {name} não declarado em :root.");
        return match.Groups["value"].Value.Trim();
    }

    [Fact]
    public void Semantic_text_tokens_should_meet_wcag_aa_in_both_themes()
    {
        var css = File.ReadAllText(FindFoundationCss());
        var light = ParseTheme(css, ":root {");
        var dark = MergeThemes(light, ParseTheme(css, ":root[data-theme=\"dark\"] {"));

        AssertTextContrast(light, "--color-text", "--color-surface-2");
        AssertTextContrast(light, "--color-text-muted", "--color-surface-2");
        AssertTextContrast(light, "--color-accent", "--color-surface-2");
        AssertTextContrast(light, "--color-good", "--color-good-bg");
        AssertTextContrast(light, "--color-warn", "--color-warn-bg");
        AssertTextContrast(light, "--color-danger", "--color-danger-bg");

        AssertTextContrast(dark, "--color-text", "--color-surface-2");
        AssertTextContrast(dark, "--color-text-muted", "--color-surface-2");
        AssertTextContrast(dark, "--color-info", "--color-info-bg");
        AssertTextContrast(dark, "--color-good", "--color-good-bg");
        AssertTextContrast(dark, "--color-warn", "--color-warn-bg");
        AssertTextContrast(dark, "--color-danger", "--color-danger-bg");
    }

    [Fact]
    public void Focus_token_should_have_non_text_contrast_against_main_surfaces()
    {
        var css = File.ReadAllText(FindFoundationCss());
        var light = ParseTheme(css, ":root {");
        var dark = MergeThemes(light, ParseTheme(css, ":root[data-theme=\"dark\"] {"));

        AssertContrast(
            ResolveToken(light, "--color-focus"),
            ResolveToken(light, "--color-bg"),
            NonTextMinimum);
        AssertContrast(
            ResolveToken(light, "--color-focus"),
            ResolveToken(light, "--color-surface-2"),
            NonTextMinimum);
        AssertContrast(
            ResolveToken(dark, "--color-focus"),
            ResolveToken(dark, "--color-bg"),
            NonTextMinimum);
        AssertContrast(
            ResolveToken(dark, "--color-focus"),
            ResolveToken(dark, "--color-surface-2"),
            NonTextMinimum);
    }

    private static void AssertTextContrast(
        IReadOnlyDictionary<string, string> tokens,
        string foreground,
        string background) =>
        AssertContrast(
            ResolveToken(tokens, foreground),
            ResolveToken(tokens, background),
            NormalTextMinimum);

    private static void AssertContrast(
        string foreground,
        string background,
        double minimum)
    {
        var ratio = ContrastRatio(foreground, background);
        Assert.True(
            ratio >= minimum,
            $"{foreground} sobre {background} tem contraste {ratio:F2}:1; mínimo {minimum:F1}:1.");
    }

    private static double ContrastRatio(string foreground, string background)
    {
        var foregroundLuminance = RelativeLuminance(foreground);
        var backgroundLuminance = RelativeLuminance(background);
        var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
        var darker = Math.Min(foregroundLuminance, backgroundLuminance);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(string hex)
    {
        var channels = Enumerable
            .Range(0, 3)
            .Select(index => int.Parse(
                hex.AsSpan(1 + index * 2, 2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture) / 255d)
            .Select(channel => channel <= 0.04045
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4))
            .ToArray();

        return 0.2126 * channels[0]
            + 0.7152 * channels[1]
            + 0.0722 * channels[2];
    }

    private static Dictionary<string, string> ParseTheme(string css, string selector)
    {
        var blockStart = css.IndexOf(selector, StringComparison.Ordinal);
        Assert.True(blockStart >= 0, $"Seletor {selector} não encontrado.");

        var blockEnd = css.IndexOf('}', blockStart);
        Assert.True(blockEnd > blockStart, $"Bloco {selector} está incompleto.");

        return TokenDeclarationRegex()
            .Matches(css[blockStart..blockEnd])
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => match.Groups["value"].Value,
                StringComparer.Ordinal);
    }

    private static Dictionary<string, string> MergeThemes(
        IReadOnlyDictionary<string, string> baseline,
        IReadOnlyDictionary<string, string> overrides)
    {
        var merged = new Dictionary<string, string>(baseline, StringComparer.Ordinal);
        foreach (var (name, value) in overrides)
            merged[name] = value;

        return merged;
    }

    private static string ResolveToken(
        IReadOnlyDictionary<string, string> tokens,
        string name,
        HashSet<string>? visited = null)
    {
        visited ??= new HashSet<string>(StringComparer.Ordinal);
        Assert.True(visited.Add(name), $"Referência circular no token {name}.");
        Assert.True(tokens.TryGetValue(name, out var value), $"Token {name} não encontrado.");

        if (value.StartsWith('#'))
            return value;

        var reference = VariableReferenceRegex().Match(value);
        Assert.True(reference.Success, $"Valor não resolvível para {name}: {value}.");
        return ResolveToken(tokens, reference.Groups["name"].Value, visited);
    }

    private static string FindFoundationCss()
    {
        var manifestPath = Path.Combine(
            AppContext.BaseDirectory,
            "PxOperations.Ui.staticwebassets.runtime.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = manifest.RootElement;
        var asset = root
            .GetProperty("Root")
            .GetProperty("Children")
            .GetProperty("css")
            .GetProperty("Children")
            .GetProperty("foundation.css")
            .GetProperty("Asset");
        var contentRoot = root
            .GetProperty("ContentRoots")[asset.GetProperty("ContentRootIndex").GetInt32()]
            .GetString()!;
        return Path.Combine(contentRoot, asset.GetProperty("SubPath").GetString()!);
    }

    [GeneratedRegex(
        @"(?<name>--[a-z0-9-]+)\s*:\s*(?<value>#[0-9a-fA-F]{6}|var\(--[a-z0-9-]+\))",
        RegexOptions.CultureInvariant)]
    private static partial Regex TokenDeclarationRegex();

    [GeneratedRegex(
        @"var\((?<name>--[a-z0-9-]+)\)",
        RegexOptions.CultureInvariant)]
    private static partial Regex VariableReferenceRegex();
}
