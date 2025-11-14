using Azure.Identity;
using Dotnet.GenAI.MyCareerAssistant;
using Dotnet.GenAI.MyCareerAssistant.Components;
using Dotnet.GenAI.MyCareerAssistant.Configuration;
using Dotnet.GenAI.MyCareerAssistant.Data;
using Dotnet.GenAI.MyCareerAssistant.Services.Ingestion;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];

if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

builder.AddServiceDefaults();

using var loggingConfigurator =
    new LoggingConfigurator();

loggingConfigurator.Configure(builder);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

await using var mcpInitializer = 
    new McpClientInitializer(
        builder.Configuration);

var mcpTools = await mcpInitializer
    .InitializeAsync();

builder.Services.AddApplicationServices(
    builder.Configuration,
    mcpTools);

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days.
    // You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

await app.InitialiseDatabaseAsync();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// By default, we ingest PDF files from the /wwwroot/Data directory.
// You can ingest from other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be
// reflected back to users or could be a source of prompt injection risk.
await DataIngestor.IngestDataAsync(
    app.Services,
    new PDFDirectorySource(
        Path.Combine(
            builder.Environment.WebRootPath, 
            "Data")));

await app.RunAsync();