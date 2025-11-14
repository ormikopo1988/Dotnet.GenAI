using Dotnet.GenAI.MyCareerAssistant.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dotnet.GenAI.MyCareerAssistant.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<QuestionAndAnswer> QuestionAndAnswers
             => Set<QuestionAndAnswer>();

        public DbSet<BusinessInquiry> BusinessInquiries
             => Set<BusinessInquiry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .ApplyConfigurationsFromAssembly(
                    typeof(Program).Assembly);
        }
    }
}
