using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
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

            // GitHub's MCP server requires an exact "application/json" Content-Type
            // and rejects the "; charset=utf-8" parameter that the SDK emits by
            // default. Route requests through a handler that strips the charset.
            // TODO: Remove this workaround once the upstream SDK stops emitting
            // "charset=utf-8" on outgoing JSON bodies. Tracking:
            // https://github.com/modelcontextprotocol/csharp-sdk/issues/792
            var httpClient = new HttpClient(new StripJsonCharsetHandler
            {
                InnerHandler = new HttpClientHandler()
            });

            _gitHubClient = await McpClient.CreateAsync(
                new HttpClientTransport(
                    new HttpClientTransportOptions
                    {
                        Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
                        Name = "github",
                        TransportMode = HttpTransportMode.StreamableHttp,
                        AdditionalHeaders = new Dictionary<string, string>
                        {
                            ["Authorization"] = $"Bearer {gitHubPat}"
                        }
                    },
                    httpClient,
                    loggerFactory: null,
                    ownsHttpClient: true));

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

            if (allowedTools.Length == 0)
            {
                return tools;
            }

            return tools
                .Where(t => 
                    allowedTools.Contains(t.Name));
        }

        private sealed class StripJsonCharsetHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var contentType = request.Content?.Headers.ContentType;

                if (contentType is not null &&
                    contentType.MediaType == "application/json")
                {
                    request.Content!.Headers.ContentType =
                        new MediaTypeHeaderValue("application/json");
                }

                return base.SendAsync(request, cancellationToken);
            }
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
