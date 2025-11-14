using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;

namespace Dotnet.GenAI.MyCareerAssistant.Extensions
{
    public static class EntityEntryExtensions
    {
        public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
            entry.References.Any(r =>
                r.TargetEntry != null &&
                r.TargetEntry.Metadata.IsOwned() &&
                (r.TargetEntry.State == EntityState.Added ||
                    r.TargetEntry.State == EntityState.Modified));
    }
}
