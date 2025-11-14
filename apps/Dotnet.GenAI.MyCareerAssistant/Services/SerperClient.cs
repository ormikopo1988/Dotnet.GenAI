using Dotnet.GenAI.MyCareerAssistant.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Services
{
    public class SerperClient
    {
        private readonly SerperSettings _settings;
        private readonly ILogger<SerperClient> _logger;
        private readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://google.serper.dev")
        };

        public SerperClient(
            SerperSettings settings,
            ILogger<SerperClient> logger)
        {
            _settings = settings;
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
        }

        public async Task<object?> WebSearchAsync(
            string query,
            CancellationToken ct = default)
        {
            _logger.LogTrace(
                "Searching for: @{query}",
                query);

            var webSearchResponse = await _httpClient.PostAsJsonAsync(
                $"/search",
                new
                {
                    q = query
                },
                ct);

            webSearchResponse.EnsureSuccessStatusCode();

            return await webSearchResponse
                .Content
                .ReadFromJsonAsync<SerperWebSearchResponse>(
                    cancellationToken: ct);
        }
    }

    public class SerperWebSearchResponse
    {
        [JsonPropertyName("organic")]
        public IEnumerable<SerperWebSearchResult> OrganicResults { get; set; } = [];
    }

    public class SerperWebSearchResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = default!;

        [JsonPropertyName("link")]
        public string Link { get; set; } = default!;
        
        [JsonPropertyName("snippet")]
        public string Snippet { get; set; } = default!;

        [JsonPropertyName("sitelinks")]
        public IEnumerable<SerperSitelinkResult> SiteLinks = [];
    }

    public class SerperSitelinkResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = default!;
    
        [JsonPropertyName("link")]
        public string Link { get; set; } = default!;
    }
}
