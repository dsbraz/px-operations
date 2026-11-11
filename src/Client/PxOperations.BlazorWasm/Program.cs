using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PxOperations.BlazorWasm;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Configuration;
using PxOperations.Ui;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = ApiBaseUrlResolver.Resolve(
    builder.Configuration["Api:BaseUrl"],
    builder.HostEnvironment.BaseAddress);

var apiScope = builder.Configuration["Api:Scope"] ?? string.Empty;

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(apiScope);
    options.ProviderOptions.LoginMode = "redirect";
});
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped(services => new HttpClient(new ApiAuthorizationMessageHandler(
    services.GetRequiredService<IAccessTokenProvider>(),
    services.GetRequiredService<NavigationManager>(),
    apiBaseAddress,
    apiScope)
{
    InnerHandler = new HttpClientHandler()
})
{
    BaseAddress = apiBaseAddress
});

// A pesquisa pública não tem login: o handler acima lançaria exceção sem token.
builder.Services.AddKeyedScoped(AnonymousApi.Key, (_, _) => new NpsClient(new HttpClient
{
    BaseAddress = apiBaseAddress
}));
builder.Services.AddScoped<ProjectHealthClient>();
builder.Services.AddScoped<ProjectsClient>();
builder.Services.AddScoped<MilestonesClient>();
builder.Services.AddScoped<NpsClient>();
builder.Services.AddScoped<TeamMembersClient>();
builder.Services.AddPxOperationsUi();

await builder.Build().RunAsync();
