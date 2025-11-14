using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Indexes;
using Dotnet.GenAI.AgentFrameworkApi.Config;
using Dotnet.GenAI.AgentFrameworkApi.Rag;
using Dotnet.GenAI.AgentFrameworkApi.Resources;
using Dotnet.GenAI.Common.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System;
using System.ClientModel;

namespace Dotnet.GenAI.AgentFrameworkApi
{
    /// <summary>
    /// Defines the Program class containing the application's entry point.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            // Add services to the container.
            builder.Services.AddProblemDetails();

            // Load the service configuration.
            var config = new ServiceConfig(builder.Configuration);

            // Add Kernel
            builder.Services.AddKernel();

            // Add AI services.
            AddAIServices(builder, config.Host);

            // Add Vector Store.
            AddVectorStore(builder, config.Host);

            // Add Agent.
            AddAgent(builder, config.Host);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseExceptionHandler();

            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// Adds AI services for chat completion and text embedding generation.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <param name="config">Service configuration.</param>
        /// <exception cref="NotSupportedException"></exception>
        private static void AddAIServices(WebApplicationBuilder builder, HostConfig config)
        {
            // Add AzureOpenAI client.
            if (config.AIChatService == AzureOpenAIChatConfig.ConfigSectionName || 
                config.Rag.AIEmbeddingService == AzureOpenAIEmbeddingsConfig.ConfigSectionName)
            {
                builder.Services.AddLogging(logging =>
                    logging.AddConsole().SetMinimumLevel(LogLevel.Information));

                builder.Services.AddSingleton(sp =>
                    LoggerFactory.Create(builder =>
                        builder.AddConsole().SetMinimumLevel(LogLevel.Trace)));

                builder.Services.AddSingleton(sp =>
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                    AzureOpenAIClient azureClient = new(
                        new Uri(config.AzureOpenAIChat.Endpoint!),
                        new ApiKeyCredential(config.AzureOpenAIChat.ApiKey!));

                    var chatClient = azureClient
                        .GetChatClient(config.AzureOpenAIChat.ModelName)
                        .AsIChatClient();

                    return new ChatClientBuilder(chatClient)
                        .UseLogging(loggerFactory)
                        .UseFunctionInvocation(loggerFactory, c =>
                        {
                            c.IncludeDetailedErrors = true;
                        })
                        .Build(sp);
                });
            }

            // Add chat completion services.
            switch (config.AIChatService)
            {
                case AzureOpenAIChatConfig.ConfigSectionName:
                    {
                        builder.Services.AddAzureOpenAIChatCompletion(
                            deploymentName: config.AzureOpenAIChat.DeploymentName,
                            apiKey: config.AzureOpenAIChat.ApiKey!,
                            endpoint: config.AzureOpenAIChat.Endpoint!);

                        break;
                    }
                default:
                    throw new NotSupportedException(
                        $"AI chat service '{config.AIChatService}' is not supported.");
            }

            // Add text embedding generation services.
            switch (config.Rag.AIEmbeddingService)
            {
                case AzureOpenAIEmbeddingsConfig.ConfigSectionName:
                    {
                        builder.Services
                            .AddAzureOpenAIEmbeddingGenerator(
                                config.AzureOpenAIEmbeddings.DeploymentName, 
                                modelId: config.AzureOpenAIEmbeddings.ModelName);
                        break;
                    }
                default:
                    throw new NotSupportedException(
                        $"AI embeddings service '{config.Rag.AIEmbeddingService}' is not supported.");
            }
        }

        /// <summary>
        /// Adds the vector store to the service collection.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <param name="config">The host configuration.</param>
        private static void AddVectorStore(WebApplicationBuilder builder, HostConfig config)
        {
            // Don't add vector store if no collection name is provided.
            // Allows for a basic experience where no data has been
            // uploaded to the vector store yet.
            if (string.IsNullOrWhiteSpace(config.Rag.CollectionName))
            {
                return;
            }

            // Add Vector Store
            switch (config.Rag.VectorStoreType)
            {
                case AzureAISearchConfig.ConfigSectionName:
                    {
                        builder.Services.AddSingleton(
                            sp => new SearchIndexClient(
                                new Uri(config.AzureAISearch.Endpoint!),
                                new AzureKeyCredential(config.AzureAISearch.ApiKey!)));

                        builder.Services.AddAzureAISearchVectorStore();

                        builder.Services
                            .AddAzureAISearchCollection<TextSnippet<string>>(config.Rag.CollectionName);

                        builder.Services.AddVectorStoreTextSearch<TextSnippet<string>>();
                        
                        break;
                    }
                default:
                    throw new NotSupportedException(
                        $"Vector store type '{config.Rag.VectorStoreType}' is not supported.");
            }
        }

        /// <summary>
        /// Adds the chat completion agent to the service collection.
        /// </summary>
        /// <param name="builder">The web application builder.</param>
        /// <param name="config">The host configuration.</param>
        private static void AddAgent(WebApplicationBuilder builder, HostConfig config)
        {
            // Register agent without RAG if no collection name is provided.
            // Allows for a basic experience where no data has
            // been uploaded to the vector store yet.
            if (string.IsNullOrEmpty(config.Rag.CollectionName))
            {
                var templateConfig = KernelFunctionYaml
                    .ToPromptTemplateConfig(
                        EmbeddedResource.Read("AgentDefinition.yaml"));

                builder.Services.AddTransient((sp) =>
                {
                    return new ChatCompletionAgent(
                        templateConfig, 
                        new HandlebarsPromptTemplateFactory())
                    {
                        Kernel = sp.GetRequiredService<Kernel>(),
                    };
                });
            }
            else
            {
                // Register agent with RAG.
                var templateConfig = KernelFunctionYaml
                    .ToPromptTemplateConfig(
                        EmbeddedResource.Read("AgentWithRagDefinition.yaml"));

                switch (config.Rag.VectorStoreType)
                {
                    case AzureAISearchConfig.ConfigSectionName:
                        {
                            AddAgentWithRag<string>(builder, templateConfig);
                            break;
                        }
                    default:
                        throw new NotSupportedException(
                            $"Vector store type '{config.Rag.VectorStoreType}' is not supported.");
                }
            }

            static void AddAgentWithRag<TKey>(
                WebApplicationBuilder builder, 
                PromptTemplateConfig templateConfig)
            {
                builder.Services.AddTransient((sp) =>
                {
                    var kernel = sp.GetRequiredService<Kernel>();

                    var vectorStoreTextSearch = sp
                        .GetRequiredService<VectorStoreTextSearch<TextSnippet<TKey>>>();

                    // Add a search plugin to the kernel which we will use in the agent template
                    // to do a vector search for related information to the user query.
                    kernel.Plugins.Add(
                        vectorStoreTextSearch
                            .CreateWithGetTextSearchResults("SearchPlugin"));

                    return new ChatCompletionAgent(
                        templateConfig, 
                        new HandlebarsPromptTemplateFactory())
                    {
                        Kernel = kernel,
                    };
                });
            }
        }
    }
}
