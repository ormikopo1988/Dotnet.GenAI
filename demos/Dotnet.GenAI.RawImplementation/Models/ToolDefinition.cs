namespace Dotnet.GenAI.RawImplementation.Models
{
    public class ToolDefinition
    {
        public string Type { get; set; } = "function";

        public required FunctionDefinition Function { get; set; }
    }

    public class FunctionDefinition
    {
        public required string Name { get; set; }

        public required string Description { get; set; }

        public required object Parameters { get; set; }
    }
}
