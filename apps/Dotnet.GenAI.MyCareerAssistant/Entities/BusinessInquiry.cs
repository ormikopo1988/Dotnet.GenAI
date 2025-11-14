namespace Dotnet.GenAI.MyCareerAssistant.Entities
{
    public class BusinessInquiry : BaseEmbeddableEntity
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Request { get; set; } = string.Empty;
    }
}
