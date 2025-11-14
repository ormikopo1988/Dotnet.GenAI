using Azure.AI.OpenAI;
using Dotnet.GenAI.MyCareerAssistant.BackgroundServices;
using Dotnet.GenAI.MyCareerAssistant.Configuration;
using Dotnet.GenAI.MyCareerAssistant.Data;
using Dotnet.GenAI.MyCareerAssistant.Data.Interceptors;
using Dotnet.GenAI.MyCareerAssistant.Interfaces;
using Dotnet.GenAI.MyCareerAssistant.Services;
using Dotnet.GenAI.MyCareerAssistant.Services.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System;
using System.ClientModel;
using System.Collections.Generic;

namespace Dotnet.GenAI.MyCareerAssistant
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IReadOnlyCollection<McpClientTool> mcpClientTools)
        {
            services.AddAIServices(configuration, mcpClientTools);
            services.AddAppServices(configuration);
            services.AddBackgroundServices();
            services.AddData(configuration);
            services.AddInjectionServices(configuration);
            services.AddEmailSender(configuration);
            services.AddSerperClient(configuration);

            return services;
        }

        private static IServiceCollection AddAIServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IReadOnlyCollection<McpClientTool> mcpClientTools)
        {
            var azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"] ??
                throw new InvalidOperationException(
                    "Missing configuration: AzureOpenAi:Endpoint.");

            var azureOpenAIKey = configuration["AzureOpenAI:Key"] ??
                throw new InvalidOperationException(
                    "Missing configuration: AzureOpenAi:Key.");

            var azureOpenAIChatModelDeploymentName = 
                configuration["AzureOpenAI:ChatModelDeploymentName"] ??
                    throw new InvalidOperationException(
                        "Missing configuration: AzureOpenAi:ChatModelDeploymentName.");

            var azureOpenAIEmbeddingModelDeploymentName = 
                configuration["AzureOpenAI:EmbeddingModelDeploymentName"] ??
                    throw new InvalidOperationException(
                        "Missing configuration: AzureOpenAi:EmbeddingModelDeploymentName.");

            var azureOpenAi = new AzureOpenAIClient(
                new Uri(azureOpenAIEndpoint),
                new ApiKeyCredential(azureOpenAIKey));

            services.AddSingleton(azureOpenAi);

            services.AddSingleton(sp =>
            {
                var loggerFactory = sp
                    .GetRequiredService<ILoggerFactory>();

                var azureClient = sp
                    .GetRequiredService<AzureOpenAIClient>();

                var chatClient = azureClient
                    .GetChatClient(azureOpenAIChatModelDeploymentName)
                    .AsIChatClient();

                return new ChatClientBuilder(chatClient)
                    .UseLogging(loggerFactory)
                    .UseFunctionInvocation(loggerFactory, c =>
                    {
                        c.IncludeDetailedErrors = true;
                    })
                    .Build(sp);
            });

            services.AddTransient(sp => new ChatOptions
            {
                Tools = 
                [
                    .. FunctionRegistry.GetTools(sp), 
                    .. mcpClientTools
                ],
                ModelId = azureOpenAIChatModelDeploymentName
            });

            var embeddingGenerator = azureOpenAi
                .GetEmbeddingClient(
                    azureOpenAIEmbeddingModelDeploymentName)
                .AsIEmbeddingGenerator();

            services.AddEmbeddingGenerator(embeddingGenerator);

            return services;
        }

        private static IServiceCollection AddAppServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var systemPromptSettings = new SystemPromptSettings();
            configuration.Bind(
                SystemPromptSettings.SystemPromptSectionKey,
                systemPromptSettings);
            services.AddSingleton(systemPromptSettings);

            services
                .AddScoped<ISystemPromptGenerator, SystemPromptGenerator>();

            services
                .AddSingleton<ISuggestionPromptGenerator, SuggestionPromptGenerator>();

            services
                .AddScoped<IQuestionAndAnswerService, QuestionAndAnswerService>();

            services
                .AddScoped<IBusinessInquiryService, BusinessInquiryService>();

            services
                .AddScoped<ICosineSimilarityService, CosineSimilarityService>();

            return services;
        }

        private static IServiceCollection AddBackgroundServices(
            this IServiceCollection services)
        {
            services.AddHostedService<DataIngestionService>();

            return services;
        }

        private static IServiceCollection AddData(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration
                .GetConnectionString("DefaultConnection") ??
                throw new ArgumentNullException("DefaultConnection");

            services.AddSingleton(TimeProvider.System);

            services
                .AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

            services.AddDbContext<ApplicationDbContext>(
                (sp, options) => options
                    .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())
                    .UseNpgsql(
                        connectionString: connectionString, 
                        npgsqlOptions => 
                            npgsqlOptions
                                .UseQuerySplittingBehavior(
                                    QuerySplittingBehavior.SingleQuery))
                    .UseSnakeCaseNamingConvention());

            services.AddScoped<ApplicationDbContext>();

            services.AddScoped<ApplicationDbContextInitialiser>();

            return services;
        }

        private static IServiceCollection AddInjectionServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var vectorStoreConnectionString = configuration
                .GetConnectionString("DefaultConnection") ??
                throw new ArgumentNullException("DefaultConnection");

            services.AddPostgresVectorStore(vectorStoreConnectionString);

            services.AddPostgresCollection<string, IngestedChunk>(
                "data-dotnet_genai_mycareerassistant-chunks",
                vectorStoreConnectionString);

            services.AddPostgresCollection<string, IngestedDocument>(
                "data-dotnet_genai_mycareerassistant-documents",
                vectorStoreConnectionString);

            services.AddScoped<DataIngestor>();
            services.AddSingleton<SemanticSearch>();

            return services;
        }

        private static IServiceCollection AddEmailSender(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var emailSenderSettings = new EmailSenderSettings();
            configuration.Bind(
                EmailSenderSettings.EmailSenderSectionKey,
                emailSenderSettings);
            services.AddSingleton(emailSenderSettings);

            services.AddSingleton<EmailSender>();

            return services;
        }

        private static IServiceCollection AddSerperClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var serperSettings = new SerperSettings();
            configuration.Bind(
                SerperSettings.SerperSectionKey,
                serperSettings);
            services.AddSingleton(serperSettings);

            services.AddSingleton<SerperClient>();

            return services;
        }
    }
}
