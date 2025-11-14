using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace Dotnet.GenAI.Common.Configuration
{
    /// <summary>
    /// Helper class for loading host configuration settings.
    /// </summary>
    public sealed class HostConfig
    {
        /// <summary>
        /// The AI services section name.
        /// </summary>
        public const string AIServicesSectionName = "AIServices";

        /// <summary>
        /// The Vector stores section name.
        /// </summary>
        public const string VectorStoresSectionName = "VectorStores";

        /// <summary>
        /// The name of the connection string of Azure OpenAI service.
        /// </summary>
        public const string AzureOpenAIConnectionStringName = "AzureOpenAI";

        private readonly AzureOpenAIChatConfig _azureOpenAIChatConfig = new();

        private readonly AzureOpenAIEmbeddingsConfig _azureOpenAIEmbeddingsConfig = new();

        private readonly AzureAISearchConfig _azureAISearchConfig = new();

        private readonly RagConfig _ragConfig = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HostConfig"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        public HostConfig(ConfigurationManager configurationManager)
        {
            configurationManager
                .GetSection($"{AIServicesSectionName}:{AzureOpenAIChatConfig.ConfigSectionName}")
                .Bind(_azureOpenAIChatConfig);
            configurationManager
                .GetSection($"{AIServicesSectionName}:{AzureOpenAIEmbeddingsConfig.ConfigSectionName}")
                .Bind(_azureOpenAIEmbeddingsConfig);
            configurationManager
                .GetSection($"{VectorStoresSectionName}:{AzureAISearchConfig.ConfigSectionName}")
                .Bind(_azureAISearchConfig);
            configurationManager
                .GetSection($"{AIServicesSectionName}:{RagConfig.ConfigSectionName}")
                .Bind(_ragConfig);
            configurationManager
                .Bind(this);
        }

        /// <summary>
        /// The AI chat service to use.
        /// </summary>
        [Required]
        public string AIChatService { get; set; } = string.Empty;

        /// <summary>
        /// The Azure OpenAI chat service configuration.
        /// </summary>
        public AzureOpenAIChatConfig AzureOpenAIChat => _azureOpenAIChatConfig;

        /// <summary>
        /// The Azure OpenAI embeddings service configuration.
        /// </summary>
        public AzureOpenAIEmbeddingsConfig AzureOpenAIEmbeddings => _azureOpenAIEmbeddingsConfig;

        /// <summary>
        /// The Azure AI search configuration.
        /// </summary>
        public AzureAISearchConfig AzureAISearch => _azureAISearchConfig;

        /// <summary>
        /// The RAG configuration.
        /// </summary>
        public RagConfig Rag => _ragConfig;
    }
}
