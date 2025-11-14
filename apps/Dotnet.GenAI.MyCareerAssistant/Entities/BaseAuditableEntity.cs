using System;

namespace Dotnet.GenAI.MyCareerAssistant.Entities
{
    public abstract class BaseAuditableEntity : BaseEntity
    {
        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
