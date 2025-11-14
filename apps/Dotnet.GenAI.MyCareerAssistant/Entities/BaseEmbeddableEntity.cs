namespace Dotnet.GenAI.MyCareerAssistant.Entities
{
    public class BaseEmbeddableEntity : BaseAuditableEntity
    {
        public string Embedding { get; set; } = string.Empty;
    }
}
