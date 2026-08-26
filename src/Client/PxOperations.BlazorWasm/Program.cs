using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PxOperations.BlazorWasm;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Configuration;
using PxOperations.Ui;

// A interface é em português e o protótipo mostra "52,6" e "11,6%". Sem fixar,
// o número segue a cultura do navegador e a mesma tela mostra ponto para uns e
// vírgula para outros. Larguras de CSS têm guarda invariante própria: "width:
// 33,3%" é inválido e a barra some sem erro nenhum.
var culture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

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
builder.Services.AddScoped<ProjectHealthClient>();
builder.Services.AddScoped<ProjectsClient>();
builder.Services.AddScoped<MilestonesClient>();
builder.Services.AddScoped<NpsClient>();
builder.Services.AddPxOperationsUi();

await builder.Build().RunAsync();
