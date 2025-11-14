using System.ComponentModel.DataAnnotations;

namespace Dotnet.GenAI.Common.Configuration
{
    /// <summary>
    /// Azure OpenAI embeddings configuration.
    /// </summary>
    public sealed class AzureOpenAIEmbeddingsConfig
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string ConfigSectionName = "AzureOpenAIEmbeddings";

        /// <summary>
        /// The name of the embeddings deployment.
        /// </summary>
        [Required]
        public string DeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// The name of the embeddings model.
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// The embeddings model version.
        /// </summary>
        public string ModelVersion { get; set; } = string.Empty;

        /// <summary>
        /// The SKU name.
        /// </summary>
        public string? SkuName { get; set; }

        /// <summary>
        /// The SKU capacity
        /// </summary>
        public int? SkuCapacity { get; set; }

        /// <summary>
        /// The API key.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// The endpoint URL.
        /// </summary>
        public string? Endpoint { get; set; }
    }
}
