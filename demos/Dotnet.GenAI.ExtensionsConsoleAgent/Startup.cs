using Azure.AI.OpenAI;
using Dotnet.GenAI.Common.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.ClientModel;

namespace Dotnet.GenAI.ExtensionsConsoleAgent
{
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

            builder.Services.AddSingleton(sp =>
                LoggerFactory.Create(builder => 
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

            builder.Services.AddSingleton(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                AzureOpenAIClient azureClient = new(
                    new Uri("https://{azureOpenAiResourceName}.openai.azure.com"), // Change this to your Azure OpenAI endpoint
                    new ApiKeyCredential(azureOpenAiKey));

                var chatClient = azureClient
                    .GetChatClient(model)
                    .AsIChatClient();

                return new ChatClientBuilder(chatClient)
                    .UseLogging(loggerFactory)
                    .UseFunctionInvocation(loggerFactory, c =>
                    {
                        c.IncludeDetailedErrors = true;
                    })
                    .Build(sp);
            });

            builder.Services.AddTransient(sp => new ChatOptions
            {
                Tools = [.. FunctionRegistry.GetTools(sp)],
                ModelId = model,
                Temperature = 1,
                MaxOutputTokens = 5000
            });

            builder.Services.AddSingleton<GithubClient>();
            builder.Services.AddSingleton<TechnologyPreferenceService>();
            builder.Services.AddSingleton<EmailService>();
        }
    }
}
