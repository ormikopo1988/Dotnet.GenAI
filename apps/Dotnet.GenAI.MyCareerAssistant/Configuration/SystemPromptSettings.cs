namespace Dotnet.GenAI.MyCareerAssistant.Configuration
{
    public sealed class SystemPromptSettings
    {
        public const string SystemPromptSectionKey = "SystemPrompt";

        public Owner Owner { get; set; } = new();
    }

    public sealed class Owner
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string GitHubUrl { get; set; } = string.Empty;

        public string MediumUrl { get; set; } = string.Empty;

        public string SessionizeUrl { get; set; } = string.Empty;
    }
}
