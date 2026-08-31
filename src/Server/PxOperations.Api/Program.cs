using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using PxOperations.Api.Errors;
using PxOperations.Api.Features.Projects;
using PxOperations.Api.Observability;
using PxOperations.Api.Serialization;
using PxOperations.Domain.Abstractions;
using PxOperations.Infrastructure.DependencyInjection;
using PxOperations.Infrastructure.Persistence;

var isOpenApiGeneration = AppDomain.CurrentDomain.FriendlyName.StartsWith(
    "GetDocument.Insider",
    StringComparison.OrdinalIgnoreCase);
if (isOpenApiGeneration)
{
    Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Insert(0, new OptionalJsonConverterFactory()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer<OptionalSchemaTransformer>();
    options.AddOperationTransformer((operation, context, ct) =>
    {
        operation.OperationId = context.Description.ActionDescriptor.RouteValues["action"];
        return Task.CompletedTask;
    });
});
builder.Services.AddCors(options =>
{
    var clientOrigins = builder.Configuration.GetSection("Cors:ClientOrigins").Get<string[]>()
        ?? ["http://localhost:8080"];
    options.AddPolicy("ClientDevelopment", policy =>
    {
        policy.WithOrigins(clientOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddApiOpenTelemetry(builder.Configuration, builder.Environment);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectRequestValidator>();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;

    // A API roda atrás do front-end do Cloud Run, cujo endereço nunca é
    // loopback. Confiar só em loopback aqui — que já é o padrão do framework —
    // fazia UseForwardedHeaders descartar o X-Forwarded-For, e o rate limit do
    // link público passava a particionar pelo endereço compartilhado da
    // plataforma: um balde por link, não por cliente. O contêiner só recebe
    // tráfego através desse front-end, que acrescenta o IP real ao cabeçalho,
    // então confiar nele é seguro. ForwardLimit continua em 1 para ler a
    // entrada mais à direita, a que a plataforma escreveu e o cliente não
    // consegue forjar prefixando o próprio X-Forwarded-For.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = 1;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        await Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too Many Requests",
            detail: "Too many attempts. Try again later.").ExecuteAsync(context.HttpContext);
    };
    options.AddPolicy("nps-public", httpContext =>
    {
        var token = httpContext.Request.RouteValues["token"]?.ToString() ?? "invalid";
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter($"{token}:{ip}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    if (!isOpenApiGeneration && app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }

    app.MapOpenApi();
    app.UseCors("ClientDevelopment");
}

app.MapControllers();

app.Run();

public partial class Program;
