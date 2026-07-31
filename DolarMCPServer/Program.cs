using DolarMcpServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// -----------------------------------------------------------------------
// IMPORTANTE: el transporte es stdio, así que stdout queda reservado
// exclusivamente para los mensajes JSON-RPC del protocolo MCP.
// Todo el logging se redirige a stderr para no corromper el stream.
// -----------------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// HttpClient tipado para el servicio de cotizaciones, con timeout y base address.
builder.Services.AddHttpClient<IDolarService, DolarService>(client =>
{
    client.BaseAddress = new Uri("https://dolarapi.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Registro del servidor MCP: transporte stdio + herramientas descubiertas
// automáticamente por reflexión en el ensamblado actual (Tools/DolarTools.cs).
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();

await host.RunAsync();
