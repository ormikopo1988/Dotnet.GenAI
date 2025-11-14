using dotenv.net;
using Dotnet.GenAI.RawImplementation.Models;
using Dotnet.GenAI.RawImplementation.Services;
using System;
using System.Collections.Generic;
using System.Text.Json;

DotEnv.Load();

var azureOpenAiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? 
    throw new InvalidOperationException("Missing AZURE_OPENAI_API_KEY");

var weatherTool = new ToolDefinition
{
    Function = new FunctionDefinition
    {
        Name = "get_weather",
        Description =
            "Retrieves current weather for the given location. Use this when the user asks about weather conditions, temperature, or climate in a specific place.",
        Parameters = new
        {
            type = "object",
            properties = new
            {
                location = new
                {
                    type = "string",
                    description = "The city and state or country, e.g., 'Athens, GR'"
                }
            },
            required = new[] { "location" }
        }
    }
};

var availableTools = new List<ToolDefinition> { weatherTool };

List<ChatMessage> messages =
[
    new ChatMessage
    {
        Role = ChatRole.System,
        Content = "You are a helpful AI assistant. Be concise and friendly."
    },
    new ChatMessage
    {
        Role = ChatRole.Assistant,
        Content = "Hello! What would you like to do today?"
    }
];

Console.WriteLine($"Assistant: {messages[1].Content}\n");

var weatherService = new WeatherService();

var aiService = new AzureOpenAiService(azureOpenAiKey);

while (true)
{
    Console.ForegroundColor = ConsoleColor.Blue;
    
    Console.Write("User: ");
    
    var input = Console.ReadLine();
    
    Console.ResetColor();

    if (input == null || input?.ToLower() == "exit")
    {
        Console.WriteLine("\nGoodbye!");
        break;
    }

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    messages.Add(new ChatMessage
    {
        Role = ChatRole.User,
        Content = input!
    });

    try
    {
        var response = await aiService.CompleteChat(
            messages,
            availableTools);

        messages.Add(response);

        // Check if LLM wants to call a function
        if (response.ToolCalls?.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[AI is calling a function...]\n");
            Console.ResetColor();

            // Process each function call
            foreach (var toolCall in response.ToolCalls)
            {
                var functionName = toolCall.Function.Name;
                var argumentsJson = toolCall.Function.Arguments;

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Function: {functionName}");
                Console.WriteLine($"Arguments: {argumentsJson}");
                Console.ResetColor();

                // Execute the function
                string functionResult;

                if (functionName == "get_weather")
                {
                    // Parse arguments
                    var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson);
                    var location = arguments?["location"]?.ToString() ?? "Unknown";

                    // Call the actual function
                    functionResult = await weatherService.GetWeather(location);
                }
                else
                {
                    functionResult = JsonSerializer.Serialize(new
                    {
                        error = $"Unknown function: {functionName}"
                    });
                }

                // Add function result to conversation
                messages.Add(new ChatMessage
                {
                    Role = ChatRole.Tool,
                    Content = functionResult,
                    ToolCallId = toolCall.Id
                });
            }

            // Call LLM again with function results to get final response
            var finalResponse = await aiService.CompleteChat(
                messages,
                tools: availableTools);

            messages.Add(finalResponse);

            // Display final response
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nAssistant: {finalResponse.Content}\n");
            Console.ResetColor();
        }
        else
        {
            // No function call, just display the text response
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nAssistant: {response.Content}\n");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        
        Console.WriteLine($"\nError: {ex.Message}\n");
        
        Console.ResetColor();
    }
}

