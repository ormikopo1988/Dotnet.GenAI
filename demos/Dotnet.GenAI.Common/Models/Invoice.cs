using System;

namespace Dotnet.GenAI.Common.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        public required string Description { get; set; }
        
        public required decimal Amount { get; set; }
        
        public required DateTime Date { get; set; }
        
        public required string Status { get; set; }
        
        public required DateTime Due { get; set; }
    }
}
