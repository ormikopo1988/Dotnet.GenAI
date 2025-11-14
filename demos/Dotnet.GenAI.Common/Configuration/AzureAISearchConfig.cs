namespace Dotnet.GenAI.Common.Configuration
{
    /// <summary>
    /// Azure AI Search service settings.
    /// </summary>
    public sealed class AzureAISearchConfig
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string ConfigSectionName = "AzureAISearch";

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
