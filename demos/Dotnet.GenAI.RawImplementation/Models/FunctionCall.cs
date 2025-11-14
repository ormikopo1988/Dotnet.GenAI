namespace Dotnet.GenAI.RawImplementation.Models
{
    public class FunctionCall
    {
        public required string Name { get; set; }

        public required string Arguments { get; set; }
    }
}
