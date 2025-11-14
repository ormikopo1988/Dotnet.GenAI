using Dotnet.GenAI.MyCareerAssistant.Interfaces;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Services
{
    public class SuggestionPromptGenerator : ISuggestionPromptGenerator
    {
        public async Task<string> GenerateSuggestionPromptAsync(
            CancellationToken ct = default)
        {
            return await File.ReadAllTextAsync(
                "./PromptTemplates/suggestion-prompt.md",
                ct);
        }
    }
}
