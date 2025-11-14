using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Interfaces
{
    public interface ISuggestionPromptGenerator
    {
        Task<string> GenerateSuggestionPromptAsync(
            CancellationToken ct = default);
    }
}
