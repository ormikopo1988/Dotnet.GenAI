using Dotnet.GenAI.MyCareerAssistant.Dtos;
using Dotnet.GenAI.MyCareerAssistant.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Interfaces
{
    public interface IQuestionAndAnswerService
    {
        Task<int> CreateAsync(
            QuestionAndAnswerOptions options,
            CancellationToken ct = default);

        Task<IEnumerable<QuestionAndAnswerDto>>
            GetAllAsync(CancellationToken ct = default);

        Task<QuestionAndAnswerDto?>
            GetBySemanticMeaningAsync(
                string question,
                CancellationToken ct = default);
    }
}
