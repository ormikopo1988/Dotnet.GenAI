namespace Dotnet.GenAI.MyCareerAssistant.Entities
{
    public class QuestionAndAnswer : BaseEmbeddableEntity
    {
        public string Question { get; set; } = string.Empty;

        public string? Answer { get; set; }
    }
}
