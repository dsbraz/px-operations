using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PxOperations.Api;
using Testcontainers.PostgreSql;

namespace PxOperations.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(ApiIntegrationCollection.Name)]
public sealed class ApiIntegrationCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "api-integration";
}

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("pxoperations")
        .WithUsername("pxoperations")
        .WithPassword("pxoperations")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

public sealed class ApiWebApplicationFactory(
    string connectionString,
    TimeProvider? timeProvider = null,
    string? clientOrigin = null,
    ILoggerProvider? loggerProvider = null,
    string tenantId = TestTokens.TenantId,
    string clientId = TestTokens.ClientId) : WebApplicationFactory<Program>
{
    // Todo client nasce autenticado como membro da BRQ, para que os testes de
    // cada módulo continuem falando só do módulo. Quem testa a autenticação
    // limpa ou troca o cabeçalho.
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Member());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Database:MigrateOnStartup"] = "true",
                ["Authentication:TenantId"] = tenantId,
                ["Authentication:ClientId"] = clientId
            };
            if (clientOrigin is not null)
            {
                settings["Cors:ClientOrigins:0"] = clientOrigin;
            }

            configuration.AddInMemoryCollection(settings);
        });
        // Só a chave de assinatura é de teste: issuer, audience e a política de
        // autorização seguem os de produção.
        builder.ConfigureServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration();
                configuration.SigningKeys.Add(TestTokens.SigningKey);
                options.Configuration = configuration;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            }));
        if (timeProvider is not null)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(timeProvider);
            });
        }

        // Contar comandos pelo log do EF evita reconfigurar o DbContext no teste
        // e, com isso, evita divergir da configuração de produção.
        if (loggerProvider is not null)
        {
            builder.ConfigureLogging(logging =>
            {
                logging.AddProvider(loggerProvider);
                logging.AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Information);
            });
        }
    }
}
