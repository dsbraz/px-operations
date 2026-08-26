using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using PxOperations.Api.Features.Nps;
using PxOperations.Api.Features.Projects;
using PxOperations.Api.Observability;
using PxOperations.Api.Serialization;
using PxOperations.Domain.Abstractions;
using PxOperations.Infrastructure.DependencyInjection;
using PxOperations.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
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
    options.AddPolicy("ClientDevelopment", policy =>
    {
        policy.WithOrigins("http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// B4: limite por IP só no envio público de resposta. Atrás do Cloud Run o IP
// real vem do X-Forwarded-For, que ASPNETCORE_FORWARDEDHEADERS_ENABLED já
// resolve (scripts/cloudrun-api.yaml.template) — não repetir UseForwardedHeaders
// aqui, ou a cadeia é processada duas vezes.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AntiAbuse.SubmitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
        // Sob TestServer não há IP remoto. Sem o fallback a partição recebe
        // chave nula e o limiter estoura.
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = AntiAbuse.SubmitPermitLimit,
            Window = AntiAbuse.SubmitWindow
        }));
});
builder.Services.AddApiOpenTelemetry(builder.Configuration, builder.Environment);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProjectRequestValidator>();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    app.MapOpenApi();
    app.UseCors("ClientDevelopment");
}

app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program;
