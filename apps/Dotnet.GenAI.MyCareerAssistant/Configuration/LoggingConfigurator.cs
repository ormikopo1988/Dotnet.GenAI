using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Dotnet.GenAI.MyCareerAssistant.Configuration
{
    public sealed class LoggingConfigurator : IDisposable
    {
        private TracerProvider? _tracerProvider;
        private MeterProvider? _meterProvider;
        private ILoggerFactory? _loggerFactory;
        
        public void Configure(WebApplicationBuilder builder)
        {
            var appInsightsConnectionString =
                builder.Configuration["ApplicationInsights:ConnectionString"] ??
                builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

            var resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService(
                    builder.Configuration["ApplicationInsights:CloudRoleName"] ??
                    "Dotnet.GenAI.MyCareerAssistant");

            var resourceAttributes = new Dictionary<string, object>
            {
                { "service.instance.id",
                    Environment.MachineName.ToLower(
                        CultureInfo.CurrentCulture) }
            };

            resourceBuilder
                .AddAttributes(resourceAttributes);

            // Enable model diagnostics with sensitive data.
            AppContext.SetSwitch(
                "Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive",
                true);

            var tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource("Microsoft.SemanticKernel*");

            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                tracerProviderBuilder
                    .AddAzureMonitorTraceExporter(options =>
                        options.ConnectionString = appInsightsConnectionString);
            }
            else             
            {
                tracerProviderBuilder
                    .AddConsoleExporter();
            }

            _tracerProvider = tracerProviderBuilder.Build();

            var meterProviderBuilder = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddMeter("Microsoft.SemanticKernel*");

            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                meterProviderBuilder
                    .AddAzureMonitorMetricExporter(options =>
                        options.ConnectionString = appInsightsConnectionString);
            }
            else
            {
                meterProviderBuilder
                    .AddConsoleExporter();
            }

            _meterProvider = meterProviderBuilder.Build();

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                // Add OpenTelemetry as a logging provider
                builder.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(resourceBuilder);

                    if (!string.IsNullOrEmpty(appInsightsConnectionString))
                    {
                        options.AddAzureMonitorLogExporter(options =>
                            options.ConnectionString = appInsightsConnectionString);
                    }
                    else
                    {
                        options.AddConsoleExporter();
                    }

                    // Format log messages. This is default to false.
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });

            builder.Services.AddSingleton(_loggerFactory);
        }

        public void Dispose()
        {
            if (_tracerProvider is not null)
            {
                _tracerProvider.Dispose();

                _tracerProvider = null;
            }

            if (_meterProvider is not null)
            {
                _meterProvider.Dispose();

                _meterProvider = null;
            }

            if (_loggerFactory is not null)
            {
                _loggerFactory.Dispose();

                _loggerFactory = null;
            }
        }
    }
}
