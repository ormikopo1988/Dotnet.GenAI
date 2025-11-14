using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Dotnet.GenAI.SemanticKernelConsoleAgent
{
    public static class ChatAgent
    {
        private static readonly ActivitySource ActivitySource = 
            new(nameof(IChatCompletionService));

        public static async Task RunAsync(IServiceProvider sp)
        {
            var client = sp.GetRequiredService<IChatCompletionService>();

            ChatHistory history = [];

            history.AddSystemMessage(
                "You are a helpful CLI assistant. Use the provided functions when appropriate.");

            Console.WriteLine($"Assistant > Ask me anything (empty = exit).");

            int turnsSinceLastSummary = 0;

            const int SUMMARY_INTERVAL = 5;

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Blue;

                Console.Write("User > ");

                var input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                {
                    break;
                }
                
                Console.ResetColor();

                history.AddUserMessage(input);

                var kernel = sp.GetRequiredService<Kernel>();

                var promptExecutionSettings = new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                };

                ChatMessageContent response;

                using var activity = ActivitySource
                    .StartActivity(nameof(RunAsync));

                activity?.SetBaggage(nameof(ChatAgent), nameof(RunAsync));

                response = await client
                    .GetChatMessageContentAsync(
                        history,
                        promptExecutionSettings,
                        kernel);

                Console.WriteLine($"Assistant > {response.Content}");

                //var streamedResponse = client.GetStreamingChatMessageContentsAsync(
                //    history,
                //    promptExecutionSettings,
                //    kernel);

                //var aggregatedContent = new StringBuilder();

                //Console.Write("Assistant > ");

                //await foreach (var chunk in streamedResponse)
                //{
                //    Console.Write(chunk);

                //    aggregatedContent.Append(chunk);

                //    await Task.Delay(10);
                //}

                //response = new ChatMessageContent(
                //    AuthorRole.Assistant, 
                //    aggregatedContent.ToString());

                //history.AddMessage(
                //    response.Role,
                //    response.Content ?? string.Empty);

                //Console.WriteLine();

                turnsSinceLastSummary++;

                if (turnsSinceLastSummary >= SUMMARY_INTERVAL)
                {
                    var summary = await SummarizeHistory(
                        history,
                        client);

                    history =
                    [
                        history[0],
                        new ChatMessageContent(AuthorRole.System, summary)
                    ];

                    turnsSinceLastSummary = 0;
                }
            }

            static async Task<string> SummarizeHistory(
                ChatHistory history, 
                IChatCompletionService client)
            {
                var summaryPrompt = 
                    "Summarize the following conversation in a few sentences:\n\n";
            
                foreach (var msg in history)
                {
                    summaryPrompt += $"{msg.Role}: {msg.Content}\n";
                }
                
                var summaryResponse = await client
                    .GetChatMessageContentAsync(summaryPrompt);

                return summaryResponse.Content!;
            }
        }
    }
}
