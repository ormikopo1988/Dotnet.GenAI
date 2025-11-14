using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dotnet.GenAI.ExtensionsConsoleAgent
{
    public static class ChatAgent
    {
        public static async Task RunAsync(IServiceProvider sp)
        {
            var client = sp.GetRequiredService<IChatClient>();

            var chatOptions = sp.GetRequiredService<ChatOptions>();

            var history = new List<ChatMessage>
            {
                new(
                    ChatRole.System, 
                    "You are a helpful CLI assistant. Use the provided functions when appropriate.")
            };

            Console.WriteLine($"Ask me anything (empty = exit).");

            int turnsSinceLastSummary = 0;

            const int SUMMARY_INTERVAL = 5;

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Blue;

                var input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                {
                    break;
                }
                
                Console.ResetColor();

                history.Add(new ChatMessage(ChatRole.User, input));

                var response = await client
                    .GetResponseAsync(history, chatOptions);

                Console.WriteLine(response.Text);

                history.AddRange(response.Messages);

                turnsSinceLastSummary++;

                if (turnsSinceLastSummary >= SUMMARY_INTERVAL)
                {
                    var summary = await SummarizeHistoryAsync(
                        history, 
                        client, 
                        chatOptions);

                    history = 
                    [
                        history[0],
                        new ChatMessage(ChatRole.System, summary)
                    ];

                    turnsSinceLastSummary = 0;
                }
            }

            static async Task<string> SummarizeHistoryAsync(
                List<ChatMessage> history, 
                IChatClient client, 
                ChatOptions chatOptions)
            {
                var summaryPrompt = 
                    "Summarize the following conversation in a few sentences:\n\n";
            
                foreach (var msg in history)
                {
                    summaryPrompt += $"{msg.Role}: {msg.Text}\n";
                }
                
                var summaryHistory = new List<ChatMessage>
                {
                    new(ChatRole.System, summaryPrompt)
                };

                var summaryResponse = await client
                    .GetResponseAsync(summaryHistory, chatOptions);

                return summaryResponse.Text;
            }
        }
    }
}
