using Dotnet.GenAI.MyCareerAssistant.Entities;
using Dotnet.GenAI.MyCareerAssistant.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Data.Interceptors
{
    public class AuditableEntityInterceptor : SaveChangesInterceptor
    {
        private readonly TimeProvider _dateTime;

        public AuditableEntityInterceptor(
            TimeProvider dateTime)
        {
            _dateTime = dateTime;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);

            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }

        public void UpdateEntities(DbContext? context)
        {
            if (context is null)
            {
                return;
            }

            foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
            {
                if (entry.State is EntityState.Added or EntityState.Modified ||
                    entry.HasChangedOwnedEntities())
                {
                    var utcNow = _dateTime.GetUtcNow();
                    
                    if (entry.State == EntityState.Added)
                    {
                        entry.Entity.CreatedAt = utcNow;
                    }
                    
                    entry.Entity.UpdatedAt = utcNow;
                }
            }
        }
    }
}
