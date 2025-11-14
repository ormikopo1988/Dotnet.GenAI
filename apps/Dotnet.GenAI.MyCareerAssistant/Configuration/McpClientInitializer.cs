using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Configuration
{
    /// <summary>
    /// Handles creation and disposal of all MCP clients, 
    /// and filters tools according to configuration.
    /// </summary>
    public sealed class McpClientInitializer : IAsyncDisposable
    {
        private readonly IConfiguration _configuration;
        private McpClient? _playwrightClient;
        private McpClient? _gitHubClient;

        public McpClientInitializer(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IReadOnlyCollection<McpClientTool>> 
            InitializeAsync()
        {
            var allTools = new List<McpClientTool>();

            try
            {
                allTools.AddRange(
                    await InitializePlaywrightClientAsync());
               
                allTools.AddRange(
                    await InitializeGitHubClientAsync());
            }
            catch (Exception ex)
            {
                await DisposeAsync();

                throw new InvalidOperationException(
                    "Failed to initialize MCP clients", 
                    ex);
            }

            return allTools;
        }

        private async Task<IEnumerable<McpClientTool>> 
            InitializePlaywrightClientAsync()
        {
            _playwrightClient = await McpClient.CreateAsync(
                new StdioClientTransport(new()
                {
                    Command = "npx",
                    Arguments = 
                    [
                        "@playwright/mcp@latest",
                        //"--headless",
                        "--no-sandbox", 
                        "--isolated"
                    ],
                    Name = "playwright"
                }));

            var playwrightTools = await _playwrightClient
                .ListToolsAsync();

            return FilterAllowedTools(
                playwrightTools, 
                "AllowedPlaywrightTools");
        }

        private async Task<IEnumerable<McpClientTool>>
            InitializeGitHubClientAsync()
        {
            var gitHubPat = _configuration["GitHubPat"];

            if (string.IsNullOrWhiteSpace(gitHubPat))
            {
                return [];
            }

            _gitHubClient = await McpClient.CreateAsync(
                new HttpClientTransport(new()
                {
                    Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
                    Name = "gitHub",
                    AdditionalHeaders = new Dictionary<string, string>
                    {
                        ["Authorization"] = $"Bearer {gitHubPat}"
                    }
                }));

            var gitHubTools = await _gitHubClient
                .ListToolsAsync();

            return FilterAllowedTools(
                gitHubTools, 
                "AllowedGitHubTools");
        }

        private IEnumerable<McpClientTool> 
            FilterAllowedTools(
                IEnumerable<McpClientTool> tools,
                string configSection)
        {
            var allowedTools = _configuration
                .GetSection(configSection)
                .Get<string[]>() ?? [];

            return tools
                .Where(t => 
                    allowedTools.Contains(t.Name));
        }

        public async ValueTask DisposeAsync()
        {
            if (_playwrightClient is not null)
            {
                await _playwrightClient.DisposeAsync();

                _playwrightClient = null;
            }

            if (_gitHubClient is not null)
            {
                await _gitHubClient.DisposeAsync();

                _gitHubClient = null;
            }
        }
    }
}
