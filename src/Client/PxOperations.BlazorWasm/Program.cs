using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PxOperations.BlazorWasm;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Configuration;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = ApiBaseUrlResolver.Resolve(
    builder.Configuration["Api:BaseUrl"],
    builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = apiBaseAddress
});
builder.Services.AddScoped<HealthClient>();
builder.Services.AddScoped<ProjectHealthClient>();
builder.Services.AddScoped<ProjectsClient>();
builder.Services.AddScoped<MilestonesClient>();

await builder.Build().RunAsync();
