using Azure.AI.OpenAI;
using dotenv.net;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;

DotEnv.Load();

var azureOpenAiKey = 
    Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? 
        throw new InvalidOperationException("Missing AZURE_OPENAI_API_KEY");

AzureOpenAIClient azureClient = new(
    new Uri("https://{azureOpenAiResourceName}.openai.azure.com"), // Change this to your Azure OpenAI endpoint
    new ApiKeyCredential(azureOpenAiKey));

var chatClient = azureClient
    .GetChatClient("gpt-4.1-mini"); // Change this to your deployed model name

List<ChatMessage> messages = 
[
    new AssistantChatMessage("Hello, what do you want to do today?")
];

Console.WriteLine(messages[0].Content[0].Text);

var weatherTool = ChatTool.CreateFunctionTool(
    functionName: nameof(GetCurrentWeather),
    functionDescription: "Get the current weather in a given location",
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "location": {
                "type": "string",
                "description": "The city and state, e.g. Boston, MA"
            },
            "unit": {
                "type": "string",
                "enum": [ "celsius", "fahrenheit" ],
                "description": "The temperature unit to use. Infer this from the specified location."
            }
        },
        "required": [ "location" ]
    }
    """)
);

ChatCompletionOptions chatCompletionOptions = new()
{
    Tools = { weatherTool }
};

while (true)
{
    Console.ForegroundColor = ConsoleColor.Blue;

    var input = Console.ReadLine();

    if (input == null || input?.ToLower() == "exit")
    {
        break;
    }

    Console.ResetColor();

    messages.Add(new UserChatMessage(input));

    var done = false;

    ClientResult<ChatCompletion> completion = default!;

    while (!done)
    {
        completion = chatClient.CompleteChat(
            messages,
            chatCompletionOptions);

        if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
        {
            // Add a new assistant message to the conversation history that includes the tool calls
            messages.Add(new AssistantChatMessage(completion));

            foreach (ChatToolCall toolCall in completion.Value.ToolCalls)
            {
                messages.Add(
                    new ToolChatMessage(
                        toolCall.Id, 
                        GetToolCallContent(toolCall)));
            }
        }
        else
        {
            done = true;
        }
    }

    var response = completion.Value.Content[0].Text;

    messages.Add(new AssistantChatMessage(response));

    Console.WriteLine(response);
}

static string GetCurrentWeather(string location, string unit = "celsius")
{
    // Call the weather API here.
    return $"31 {unit}";
}

// Purely for convenience and clarity, this standalone local method handles tool call responses.
string GetToolCallContent(ChatToolCall toolCall)
{
    if (toolCall.FunctionName == weatherTool.FunctionName)
    {
        // Validate arguments before using them; it's not always guaranteed to be valid JSON.
        try
        {
            using JsonDocument argumentsDocument = 
                JsonDocument.Parse(
                    toolCall.FunctionArguments);

            if (!argumentsDocument
                .RootElement
                .TryGetProperty(
                    "location", 
                    out JsonElement locationElement))
            {
                Console.Error.WriteLine("Missing required \"location\" argument.");
            }
            else
            {
                var location = locationElement.GetString()!;

                if (argumentsDocument
                    .RootElement
                    .TryGetProperty(
                        "unit", 
                        out JsonElement unitElement))
                {
                    return GetCurrentWeather(
                        location, 
                        unitElement.GetString()!);
                }
                else
                {
                    return GetCurrentWeather(location);
                }
            }
        }
        catch (JsonException)
        {
            Console.Error.WriteLine("JsonException (bad arguments).");
        }
    }

    // Handle unexpected tool calls
    throw new NotImplementedException();
}