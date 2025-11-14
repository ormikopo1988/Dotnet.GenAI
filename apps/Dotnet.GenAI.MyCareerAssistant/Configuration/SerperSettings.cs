namespace Dotnet.GenAI.MyCareerAssistant.Configuration
{
    public sealed class SerperSettings
    {
        public const string SerperSectionKey = "Serper";

        public string ApiKey { get; set; } = default!;
    }
}
