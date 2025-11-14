using Dotnet.GenAI.MyCareerAssistant.Services.Ingestion;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Interfaces;

public interface IIngestionSource
{
    string SourceId { get; }

    Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(
        IReadOnlyList<IngestedDocument> existingDocuments);

    Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(
        IReadOnlyList<IngestedDocument> existingDocuments);

    Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(
        IngestedDocument document);
}
