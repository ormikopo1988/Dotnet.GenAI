using Dotnet.GenAI.RawImplementation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.RawImplementation.Services
{
    public class AzureOpenAiService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient = new();

        public AzureOpenAiService(string apiKey)
        {
            // Change this to your Azure OpenAI endpoint and deployed model name
            _httpClient.BaseAddress = new Uri(
                "https://{azureOpenAiResourceName}.openai.azure.com/openai/deployments/gpt-4.1-mini/");
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _jsonOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        }

        public async Task<ChatMessage> CompleteChat(
            List<ChatMessage> messages,
            List<ToolDefinition>? tools = null,
            string? toolChoice = null,
            CancellationToken cancellationToken = default)
        {
            var openAiRequest = new ChatRequest
            {
                Model = "gpt-4.1-mini",
                Messages = messages,
                Tools = tools,
                ToolChoice = toolChoice ?? 
                    (tools?.Count > 0 ? "auto" : null)
            };

            var jsonRequest = JsonSerializer.Serialize(
                openAiRequest, 
                _jsonOptions);

            using var content = new StringContent(
                jsonRequest, 
                Encoding.UTF8, 
                "application/json");

            try
            {
                var response = await _httpClient.PostAsync(
                    "chat/completions?api-version=2025-01-01-preview", 
                    content, 
                    cancellationToken);

                var responseContent = await response
                    .Content
                    .ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Error calling Azure OpenAI API: {response.StatusCode} - {responseContent}");
                }

                var result = JsonSerializer.Deserialize<ChatResponse>(
                    responseContent, _jsonOptions) ?? 
                        throw new InvalidOperationException(
                            "Failed to deserialize Azure OpenAI response.");

                var firstChoice = result.Choices?.FirstOrDefault();

                if (firstChoice == null || firstChoice.Message == null)
                {
                    throw new InvalidOperationException(
                        "No choices returned from Azure OpenAI API.");
                }

                return new ChatMessage
                {
                    Role = firstChoice.Message.Role,
                    Content = firstChoice.Message.Content,
                    ToolCallId = firstChoice.Message.ToolCallId,
                    ToolCalls = firstChoice.Message.ToolCalls
                };
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    "Error calling Azure OpenAI API", ex);
            }
        }
    }
}
