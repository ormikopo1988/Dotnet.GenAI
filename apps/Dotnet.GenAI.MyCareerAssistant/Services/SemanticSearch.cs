using Dotnet.GenAI.MyCareerAssistant.Services.Ingestion;
using Microsoft.Extensions.VectorData;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Services
{
    public class SemanticSearch
    {
        private readonly VectorStoreCollection<string, IngestedChunk> _vectorCollection;

        public SemanticSearch(VectorStoreCollection<string, IngestedChunk> vectorCollection)
        {
            _vectorCollection = vectorCollection;
        }

        [Description("Searches for information using a phrase or keyword")]
        public async Task<IReadOnlyList<IngestedChunk>> SearchAsync(
            [Description("The phrase to search for.")]
            string text,
            [Description("If possible, specify the filename to search that file only. If not provided or empty, the search includes all files.")]
            string? documentIdFilter,
            int maxResults)
        {
            var nearest = _vectorCollection
                .SearchAsync(
                    text,
                    maxResults,
                    new VectorSearchOptions<IngestedChunk>
                    {
                        Filter = documentIdFilter is { Length: > 0 } ?
                            record => record.DocumentId == documentIdFilter : null,
                    });

            return await nearest
                .Select(result => result.Record)
                .ToListAsync();
        }
    }
}
