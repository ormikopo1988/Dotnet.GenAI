using Dotnet.GenAI.MyCareerAssistant.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Interfaces
{
    public interface ICosineSimilarityService
    {
        Task<T?> FindMostSimilarEntityAsync<T>(
            string input,
            IEnumerable<T> entities,
            CancellationToken ct = default)
            where T : BaseEmbeddableEntity;
    }
}
