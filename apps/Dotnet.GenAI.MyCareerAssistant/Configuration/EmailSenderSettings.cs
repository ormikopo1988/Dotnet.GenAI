namespace Dotnet.GenAI.MyCareerAssistant.Configuration
{
    public sealed class EmailSenderSettings
    {
        public const string EmailSenderSectionKey = "EmailSender";

        public string ApiKey { get; set; } = default!;

        public string SenderEmail { get; set; } = default!;

        public string SenderName { get; set; } = default!;
    }
}
