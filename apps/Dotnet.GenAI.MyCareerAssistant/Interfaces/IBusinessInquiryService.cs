using Dotnet.GenAI.MyCareerAssistant.Dtos;
using Dotnet.GenAI.MyCareerAssistant.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Interfaces
{
    public interface IBusinessInquiryService
    {
        Task<int> CreateAsync(
            BusinessInquiryOptions options,
            CancellationToken ct = default);

        Task<BusinessInquiryDto?>
            GetBySemanticMeaningAsync(
                string email,
                string request,
                CancellationToken ct = default);
    }
}
