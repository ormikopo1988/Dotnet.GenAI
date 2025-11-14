using System.Collections.Generic;

namespace Dotnet.GenAI.RawImplementation.Models
{
    public class ChatRequest
    {
        public string? Model { get; set; }

        public required List<ChatMessage> Messages { get; set; } = [];

        public List<ToolDefinition>? Tools { get; set; }

        public string? ToolChoice { get; set; }
    }
}
