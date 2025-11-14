using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.Common.Services
{
    public class GithubClient
    {
        private readonly ILogger<GithubClient> _logger;
        private readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://api.github.com")
        };

        public GithubClient(ILogger<GithubClient> logger)
        {
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Add(
                HeaderNames.Accept, "application/vnd.github.v3+json");
            _httpClient.DefaultRequestHeaders.Add(
                HeaderNames.UserAgent, $"MS-Extensions-AI-Agent-{Environment.MachineName}");
        }

        public async Task<GitHubUserDetails?> GetUserInformationAsync(
            string username, CancellationToken ct = default)
        {
            _logger.LogTrace(
                "Fetch information from GitHub for user: @{username}", 
                username);

            var userDetailsResponse = await _httpClient.GetAsync(
                $"/users/{username}", 
                ct);

            if (userDetailsResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                var responseBody = await userDetailsResponse
                    .Content
                    .ReadFromJsonAsync<JsonObject>(ct);

                var message = responseBody!["message"]!.ToString();

                throw new HttpRequestException(message);
            }

            userDetailsResponse.EnsureSuccessStatusCode();

            var gitHubUserDetails = await userDetailsResponse
                .Content
                .ReadFromJsonAsync<GitHubUserDetails>(ct)!;

            var userReposResponse = await _httpClient.GetAsync(
                $"/users/{username}/repos", 
                ct);

            if (userReposResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                var responseBody = await userReposResponse
                    .Content
                    .ReadFromJsonAsync<JsonObject>(ct);

                var message = responseBody!["message"]!.ToString();

                throw new HttpRequestException(message);
            }

            userReposResponse.EnsureSuccessStatusCode();

            var userRepos = await userReposResponse
                .Content
                .ReadFromJsonAsync<List<GitHubRepoDetails>>(ct)!;

            gitHubUserDetails!.Repos = [.. userRepos!
                .OrderByDescending(ur => ur.StargazersCount)
                .Take(userRepos!.Count > 0 && userRepos.Count < 3 ? userRepos.Count : 3)];

            return gitHubUserDetails;
        }
    }

    public class GitHubUserDetails
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public List<GitHubRepoDetails> Repos { get; set; } = [];
    }

    public class GitHubRepoDetails
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;
    }
}
