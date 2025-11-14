using Dotnet.GenAI.MyCareerAssistant.Configuration;
using Dotnet.GenAI.MyCareerAssistant.Interfaces;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Services
{
    public class SystemPromptGenerator : ISystemPromptGenerator
    {
        private readonly IQuestionAndAnswerService _qaService;
        private readonly SystemPromptSettings _systemPromptSettings;

        public SystemPromptGenerator(
            IQuestionAndAnswerService qaService, 
            SystemPromptSettings systemPromptSettings)
        {
            _qaService = qaService;
            _systemPromptSettings = systemPromptSettings;
        }

        public async Task<string> GenerateSystemPromptAsync(
            CancellationToken ct = default)
        {
            var systemPromptTemplate = await File.ReadAllTextAsync(
                "./PromptTemplates/system-prompt.md",
                ct);

            var systemPrompt = systemPromptTemplate
                .Replace(
                    "{OwnerName}",
                    !string.IsNullOrWhiteSpace(_systemPromptSettings.Owner.Name) ?
                       _systemPromptSettings.Owner.Name : "N/A")
                .Replace(
                    "{OwnerEmail}",
                    !string.IsNullOrWhiteSpace(_systemPromptSettings.Owner.Email) ?
                       _systemPromptSettings.Owner.Email : "N/A")
                .Replace(
                    "{OwnerGitHubUrl}",
                    !string.IsNullOrWhiteSpace(_systemPromptSettings.Owner.GitHubUrl) ?
                       _systemPromptSettings.Owner.GitHubUrl : "N/A")
                .Replace(
                    "{OwnerMediumUrl}",
                    !string.IsNullOrWhiteSpace(_systemPromptSettings.Owner.MediumUrl) ?
                       _systemPromptSettings.Owner.MediumUrl : "N/A")
                .Replace(
                    "{OwnerSessionizeUrl}",
                    !string.IsNullOrWhiteSpace(_systemPromptSettings.Owner.SessionizeUrl) ?
                       _systemPromptSettings.Owner.SessionizeUrl : "N/A");

            var qaRecords = 
                await _qaService
                    .GetAllAsync(ct);

            var qaSection = new StringBuilder();

            foreach (var qaRecord in qaRecords)
            {
                qaSection.AppendLine(
                    $"Q: {qaRecord.Question} | A: {qaRecord.Answer}");
            }

            var qaSectionStr = qaSection
                .ToString()
                .Trim();

            var finalizedSystemPrompt = systemPrompt
                .Replace(
                    "{QuestionAndAnswerSection}",
                    !string.IsNullOrWhiteSpace(qaSectionStr)
                        ? qaSectionStr : "N/A");

            return finalizedSystemPrompt;
        }
    }
}
