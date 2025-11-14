using Dotnet.GenAI.MyCareerAssistant.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dotnet.GenAI.MyCareerAssistant.Data.Configurations
{
    public sealed class BusinessInquiryConfiguration
        : IEntityTypeConfiguration<BusinessInquiry>
    {
        public void Configure(EntityTypeBuilder<BusinessInquiry> builder)
        {
            builder
                .HasIndex(bi => bi.Email);
        }
    }
}
