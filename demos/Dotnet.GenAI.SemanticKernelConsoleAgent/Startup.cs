using Dotnet.GenAI.Common.Services;
using Dotnet.GenAI.SemanticKernelConsoleAgent.Filters;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;

namespace Dotnet.GenAI.SemanticKernelConsoleAgent
{
    // docker run --rm -it -d -p 18888:18888 -p 4317:18889 --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:latest
    public static class Startup
    {
        public static void ConfigureServices(
            HostApplicationBuilder builder,
            string model)
        {
            var azureOpenAiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ??
                throw new InvalidOperationException("Missing AZURE_OPENAI_API_KEY");

            builder.Services.AddLogging(logging => 
                logging.AddConsole().SetMinimumLevel(LogLevel.Information));

            builder.Services.AddAzureOpenAIChatCompletion(
                deploymentName: model,
                apiKey: azureOpenAiKey,
                endpoint: "https://{azureOpenAiResourceName}.openai.azure.com"); // Change this to your Azure OpenAI endpoint

            builder.Services.AddSingleton<KernelPluginCollection>(
                serviceProvider =>
                    [.. FunctionRegistry.GetPlugins(serviceProvider)]
            );

            builder.Services.AddTransient((serviceProvider) => {
                var pluginCollection = serviceProvider
                    .GetRequiredService<KernelPluginCollection>();

                return new Kernel(serviceProvider, pluginCollection);
            });

            builder.Services.AddSingleton<IFunctionInvocationFilter, LoggingFilter>();
            builder.Services.AddSingleton<IPromptRenderFilter, SafePromptFilter>();
            builder.Services.AddSingleton<IAutoFunctionInvocationFilter, EarlyTerminationFilter>();

            builder.Services.AddSingleton<GithubClient>();
            builder.Services.AddSingleton<TechnologyPreferenceService>();
            builder.Services.AddSingleton<EmailService>();
        }
    }
}
