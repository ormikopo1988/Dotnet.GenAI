using dotenv.net;
using Dotnet.GenAI.SemanticKernelConsoleAgent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;

DotEnv.Load();

string model = "gpt-4.1-mini";

var builder = Host.CreateApplicationBuilder(args);

Startup.ConfigureServices(builder, model);

// Endpoint to the Aspire Dashboard
var endpoint = "http://localhost:4317";

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService("Dotnet.GenAI.SemanticKernelConsoleAgent");

// Enable model diagnostics with sensitive data.
AppContext.SetSwitch(
    "Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", 
    true);

using var traceProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource("Microsoft.SemanticKernel*")
    .AddSource(nameof(IChatCompletionService))
    .AddOtlpExporter(options => options.Endpoint = new Uri(endpoint))
    .AddConsoleExporter()
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter("Microsoft.SemanticKernel*")
    .AddOtlpExporter(options => options.Endpoint = new Uri(endpoint))
    .AddConsoleExporter()
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    // Add OpenTelemetry as a logging provider
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resourceBuilder);
        options.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
        options.AddConsoleExporter();
        // Format log messages. This is default to false.
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddSingleton(loggerFactory);

var host = builder.Build();

await ChatAgent.RunAsync(host.Services);