using System.Collections.Generic;

namespace Dotnet.GenAI.RawImplementation.Models
{
    public class ChatMessage
    {
        public required ChatRole Role { get; set; }

        public string? Content { get; set; }

        public List<ToolCall>? ToolCalls { get; set; }

        public string? ToolCallId { get; set; }
    }
}
