using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace PxOperations.BlazorWasm.Tests.Hosting;

/// <summary>
/// Em homolog/prod o nginx gera o appsettings.json do cliente a partir de
/// ENTRA_TENANT_ID e ENTRA_CLIENT_ID na subida do contêiner. O JSON mora numa
/// string do nginx.conf.template, longe do C# que o lê: uma chave trocada só
/// apareceria como login quebrado depois do deploy.
/// </summary>
public sealed partial class NginxClientConfigurationTests
{
    private const string TenantId = "11111111-1111-1111-1111-111111111111";
    private const string ClientId = "22222222-2222-2222-2222-222222222222";

    [Fact]
    public void Nginx_should_serve_the_client_settings_built_from_the_entra_ids()
    {
        var configuration = RenderedClientConfiguration();

        Assert.Equal("/", configuration["Api:BaseUrl"]);
        Assert.Equal($"api://{ClientId}/access_as_user", configuration["Api:Scope"]);
        Assert.Equal($"https://login.microsoftonline.com/{TenantId}", configuration["AzureAd:Authority"]);
        Assert.Equal(ClientId, configuration["AzureAd:ClientId"]);
        Assert.Equal("True", configuration["AzureAd:ValidateAuthority"], ignoreCase: true);
    }

    private static IConfiguration RenderedClientConfiguration()
    {
        var template = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "src", "Client", "PxOperations.BlazorWasm", "Hosting", "nginx.conf.template"));
        var match = AppSettingsLocationRegex().Match(template);
        Assert.True(match.Success, "nginx.conf.template precisa servir /appsettings.json com return 200 '<json>'.");

        var json = match.Groups["json"].Value
            .Replace("${ENTRA_TENANT_ID}", TenantId, StringComparison.Ordinal)
            .Replace("${ENTRA_CLIENT_ID}", ClientId, StringComparison.Ordinal);
        Assert.DoesNotContain("$", json, StringComparison.Ordinal);

        return new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PX-Operations.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("PX-Operations.sln não encontrado.");
    }

    [GeneratedRegex(@"location\s*=\s*/appsettings\.json\s*\{[^}]*?return\s+200\s+'(?<json>.*?)';", RegexOptions.Singleline)]
    private static partial Regex AppSettingsLocationRegex();
}
