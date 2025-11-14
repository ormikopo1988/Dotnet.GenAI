using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Interfaces
{
    public interface ISystemPromptGenerator
    {
        Task<string> GenerateSystemPromptAsync(
            CancellationToken ct = default);
    }
}
