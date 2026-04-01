using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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

public sealed class ApiWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString
            });
        });
    }
}
