using System.Threading.Tasks;

namespace Dotnet.GenAI.Common.Services
{
    public class TechnologyPreferenceService
    {
        public Task<string[]> ListPreferences()
        {
            return Task.FromResult<string[]>([
                "C#",
                ".NET",
                "Azure",
                "DevOps",
            ]);
        }
    }
}
