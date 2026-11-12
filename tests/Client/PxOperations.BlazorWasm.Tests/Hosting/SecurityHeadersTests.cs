namespace PxOperations.BlazorWasm.Tests.Hosting;

/// <summary>
/// Quando o refresh token não serve mais, o MSAL renova o token num iframe
/// oculto: ele abre login.microsoftonline.com e volta para o login-callback do
/// próprio app. Se o CSP ou o X-Frame-Options bloquearem o iframe, a renovação
/// falha e o usuário é levado ao login no meio da tarefa. Só o nginx manda
/// esses headers, então o dev não mostra o problema.
/// </summary>
public sealed class SecurityHeadersTests
{
    [Fact]
    public void Csp_should_let_msal_renew_the_token_in_a_hidden_iframe()
    {
        var policy = ContentSecurityPolicy();

        Assert.Contains("frame-src 'self' https://login.microsoftonline.com", policy, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'self'", policy, StringComparison.Ordinal);
    }

    [Fact]
    public void Frame_options_should_allow_only_the_app_itself()
    {
        Assert.Contains("add_header X-Frame-Options \"SAMEORIGIN\" always;", Headers(), StringComparison.Ordinal);
    }

    private static string ContentSecurityPolicy()
        => Headers().Split('\n').Single(line => line.Contains("Content-Security-Policy", StringComparison.Ordinal));

    private static string Headers()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PX-Operations.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(
            directory.FullName, "src", "Client", "PxOperations.BlazorWasm", "Hosting", "security-headers.conf"));
    }
}
