using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PxOperations.Api.Authentication;

public static class AuthenticationExtensions
{
    // Entra ID emite acct=0 para membros do tenant e acct=1 para convidados
    // (B2B). Só membros da BRQ entram; a claim é opcional e precisa estar
    // habilitada no access token do app registration.
    private const string AccountTypeClaim = "acct";
    private const string MemberAccountType = "0";

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        bool isOpenApiGeneration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Lido do IConfiguration final (não do que o Program enxerga antes do
        // Build), para que ambiente, variáveis e testes possam definir o tenant.
        var jwtBearer = services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                var tenantId = configuration["Authentication:TenantId"];
                var clientId = configuration["Authentication:ClientId"];
                if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
                {
                    return;
                }

                options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                options.Audience = clientId;
                options.TokenValidationParameters.ValidIssuer = options.Authority;
                options.Events = new JwtBearerEvents { OnForbidden = WriteForbiddenProblemAsync };
            })
            .Validate(
                options => options.Authority is not null && options.Audience is not null,
                "Authentication:TenantId and Authentication:ClientId must be configured.");

        // Sem tenant não há como saber quem é da BRQ: subir assim seria subir
        // aberto. A geração do OpenAPI no build roda o Program sem configuração
        // e não atende requisições, por isso é a única exceção.
        if (!isOpenApiGeneration)
        {
            jwtBearer.ValidateOnStart();
        }

        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(AccountTypeClaim, MemberAccountType)
                .Build());

        return services;
    }

    private static async Task WriteForbiddenProblemAsync(ForbiddenContext context)
    {
        var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "Access is restricted to BRQ members."
            }
        });
    }
}
