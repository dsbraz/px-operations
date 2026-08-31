using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PxOperations.Api.IntegrationTests.Infrastructure;

namespace PxOperations.Api.IntegrationTests.Features.Nps;

[Collection(ApiIntegrationCollection.Name)]
public sealed class NpsDashboardQueryCostTests(PostgreSqlFixture fixture)
{
    /// <summary>
    /// O dashboard é recarregado a cada toggle de faceta e a cada troca de
    /// data, então o número de idas ao banco por requisição é característica
    /// observável, não detalhe interno. Este teste trava o teto.
    /// </summary>
    [Fact]
    public async Task Dashboard_should_not_scan_the_response_table_more_than_once()
    {
        var counter = new CommandCountingLoggerProvider();
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, loggerProvider: counter);
        using var client = factory.CreateClient();
        await SeedRespondedProjectAsync(client, "Query cost A");
        await SeedRespondedProjectAsync(client, "Query cost B");
        await client.GetAsync("/api/nps/dashboard");

        counter.Reset();
        await client.GetAsync("/api/nps/dashboard");

        // Eram 10: as respostas do período eram lidas uma vez para montar os
        // resultados por projeto e de novo para os indicadores, e o agregado de
        // aspectos abria uma terceira varredura da mesma tabela.
        Assert.True(counter.Count <= 8, $"O dashboard executou {counter.Count} comandos SQL.");
    }

    private static async Task SeedRespondedProjectAsync(HttpClient client, string name)
    {
        var project = await client.PostAsJsonAsync("/api/projects", new
        {
            dc = "DC1",
            status = "Em andamento",
            name,
            client = "Cliente",
            type = "Squad",
            startDate = "2026-01-01",
            endDate = "2026-12-31"
        });
        var created = await project.Content.ReadFromJsonAsync<JsonElement>();
        var projectId = created.GetProperty("id").GetInt32();

        var dispatch = await client.PostAsJsonAsync("/api/nps/dispatches", new
        {
            projectId,
            format = "complete",
            language = "pt",
            contactIds = Array.Empty<int>()
        });
        var detail = await dispatch.Content.ReadFromJsonAsync<JsonElement>();
        var token = detail.GetProperty("targets")[0].GetProperty("token").GetGuid();

        await client.PostAsJsonAsync($"/api/nps/public/{token}/responses", new
        {
            score = 9,
            quality = 5,
            schedule = 4,
            communication = 5,
            businessValue = 4,
            comment = "ok",
            respondentName = "Pessoa",
            respondentEmail = $"{Guid.NewGuid():N}@example.com"
        });
    }

    private sealed class CommandCountingLoggerProvider : ILoggerProvider
    {
        private int count;

        public int Count => Volatile.Read(ref count);

        public void Reset() => Volatile.Write(ref count, 0);

        public ILogger CreateLogger(string categoryName)
            => categoryName == DbLoggerCategory.Database.Command.Name
                ? new CountingLogger(this)
                : NullLogger.Instance;

        public void Dispose()
        {
        }

        private sealed class CountingLogger(CommandCountingLoggerProvider owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (eventId.Id == RelationalEventId.CommandExecuted.Id)
                {
                    Interlocked.Increment(ref owner.count);
                }
            }
        }
    }
}
