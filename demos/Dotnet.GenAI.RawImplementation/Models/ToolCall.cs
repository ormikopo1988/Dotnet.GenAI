namespace Dotnet.GenAI.RawImplementation.Models
{
    public class ToolCall
    {
        public required string Id { get; set; }

        public required string Type { get; set; }
        
        public required FunctionCall Function { get; set; }
    }
}
